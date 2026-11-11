using PersonalPortfolio.Api.Admin;

namespace PersonalPortfolio.Api.Assistant;

public sealed record ContactChatMessageRequest
{
    /// <summary>Message text to send to the other participant.</summary>
    public required string Content { get; init; }
}

public static class ContactChatEndpoints
{
    public const string VisitorCookieName = "PersonalPortfolio.ContactChat";
    private const int MaxMessageLength = 2000;

    public static void MapContactChatEndpoints(this WebApplication app)
    {
        app.MapGet("/api/contact-chat/session", (
            HttpContext context,
            IContactChatStore chats) =>
        {
            var session = chats.GetForVisitor(context.Request.Cookies[VisitorCookieName]);
            if (session?.Status is "Closed" or "Expired")
            {
                context.Response.Cookies.Delete(VisitorCookieName, new CookieOptions
                {
                    Path = "/api",
                    Secure = context.Request.IsHttps,
                    SameSite = context.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                });
            }

            return session is null ? Results.NotFound() : Results.Ok(session);
        })
            .AllowAnonymous()
            .WithName("GetContactChatSession")
            .WithSummary("Resume the visitor's website chat")
            .Produces<ContactChatSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/contact-chat/messages", async (
            ContactChatMessageRequest? request,
            HttpContext context,
            IContactChatStore chats,
            AssistantQuestionNotifier notifier,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request?.Content) ||
                request.Content.Length > MaxMessageLength)
            {
                return Results.BadRequest(new { error = "Provide a message between 1 and 2000 characters." });
            }

            var token = context.Request.Cookies[VisitorCookieName];
            var current = chats.GetForVisitor(token);
            if (current is null)
            {
                return Results.NotFound();
            }

            if (current.Status is "Closed" or "Expired")
            {
                return Results.Conflict(new { error = "This chat is closed or its waiting time has expired." });
            }

            var session = chats.AddVisitorMessage(token, request.Content.Trim());
            if (session is null)
            {
                return Results.Conflict(new { error = "This chat is closed or has reached its message limit." });
            }

            await notifier.NotifyContactChatMessageAsync(
                session.Id,
                "Visitor",
                request.Content.Trim(),
                cancellationToken);
            return Results.Ok(session);
        })
            .AllowAnonymous()
            .RequireRateLimiting("assistant")
            .WithName("SendVisitorContactChatMessage")
            .WithSummary("Send a message in the visitor's website chat")
            .Produces<ContactChatSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        app.MapGet("/api/admin/contact-chats", (IContactChatStore chats) =>
            Results.Ok(chats.ListForOwner()))
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .WithName("ListContactChats")
            .WithSummary("List open and closed visitor conversations")
            .Produces<IReadOnlyList<ContactChatSummary>>(StatusCodes.Status200OK);

        app.MapGet("/api/admin/contact-chats/{id:guid}", (
            Guid id,
            IContactChatStore chats) =>
        {
            var session = chats.GetForOwner(id);
            return session is null ? Results.NotFound() : Results.Ok(session);
        })
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .WithName("GetContactChat")
            .WithSummary("Get a visitor conversation")
            .Produces<ContactChatSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/admin/contact-chats/{id:guid}/messages", async (
            Guid id,
            ContactChatMessageRequest? request,
            IContactChatStore chats,
            AssistantQuestionNotifier notifier,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request?.Content) ||
                request.Content.Length > MaxMessageLength)
            {
                return Results.BadRequest(new { error = "Provide a message between 1 and 2000 characters." });
            }

            var session = chats.AddOwnerMessage(id, request.Content.Trim());
            if (session is null)
            {
                return Results.Conflict(new { error = "The conversation is missing, closed, or has reached its message limit." });
            }

            await notifier.NotifyContactChatMessageAsync(
                session.Id,
                "Abel",
                request.Content.Trim(),
                cancellationToken);
            return Results.Ok(session);
        })
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .WithName("SendOwnerContactChatMessage")
            .WithSummary("Reply to a visitor in the website chat")
            .Produces<ContactChatSession>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        app.MapPost("/api/admin/contact-chats/{id:guid}/close", (
            Guid id,
            IContactChatStore chats) =>
            chats.Close(id) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .WithName("CloseContactChat")
            .WithSummary("Close a visitor conversation")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
