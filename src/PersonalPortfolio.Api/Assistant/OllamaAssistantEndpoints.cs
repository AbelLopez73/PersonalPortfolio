using PersonalPortfolio.Api.Admin;

namespace PersonalPortfolio.Api.Assistant;

public static class OllamaAssistantEndpoints
{
    private const int MaxUserMessageLength = 2000;
    private const int MaxAssistantMessageLength = 8000;
    private const int MaxSummaryLength = 1000;
    private static readonly HashSet<string> AffirmativeResponses = new(StringComparer.OrdinalIgnoreCase)
    {
        "yes", "yeah", "yep", "sure", "okay", "ok", "sí", "si", "claro", "acepto",
        "oui", "ja", "tak", "да", "نعم", "同意", "agree", "consent",
    };

    public static void MapOllamaAssistantEndpoints(this WebApplication app)
    {
        app.MapPost("/api/assistant/chat", async (
            AssistantChatRequest? request,
            IAssistantService assistant,
            AssistantQuestionNotifier notifier,
            IContactChatStore chats,
            IAdminPresenceTracker presence,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var messages = request?.Messages;
            if (messages is null || messages.Count is < 1 or > 16 ||
                messages[^1].Role != "user" ||
                messages.Any(message =>
                    message.Role is not ("user" or "assistant") ||
                    string.IsNullOrWhiteSpace(message.Content) ||
                    message.Content.Length > (message.Role == "user" ? MaxUserMessageLength : MaxAssistantMessageLength)))
            {
                return Results.BadRequest(new { error = "Provide 1 to 16 valid chat messages; the latest message must be from the user." });
            }

            try
            {
                chats.SetOwnerOnline(presence.IsOwnerOnline);
                var modelReply = await assistant.ReplyAsync(messages, cancellationToken);
                var reply = modelReply.Reply;
                var contactChatId = default(Guid?);

                if (modelReply.StartHumanChat)
                {
                    if (!CanStartHumanChat(modelReply.VisitorName, messages))
                    {
                        reply = "I can connect you with Abel here. Please share your name and confirm that you agree to share it and this conversation with him.";
                    }
                    else
                    {
                        var existing = chats.GetForVisitor(context.Request.Cookies[ContactChatEndpoints.VisitorCookieName]);
                        if (existing is { Status: "Active" or "Waiting" })
                        {
                            contactChatId = existing.Id;
                        }
                        else
                        {
                            var access = chats.Create(modelReply.VisitorName!, messages);
                            contactChatId = access.Session.Id;
                            context.Response.Cookies.Append(
                                ContactChatEndpoints.VisitorCookieName,
                                access.Token,
                                new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = context.Request.IsHttps,
                                    SameSite = context.Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax,
                                    Path = "/api",
                                    MaxAge = TimeSpan.FromDays(30),
                                    IsEssential = true,
                                });

                            await notifier.NotifyContactRequestAsync(
                                access.Session.VisitorName,
                                access.Session.Id,
                                CreateConversationSummary(messages),
                                cancellationToken);
                        }
                    }
                }

                await notifier.NotifyAsync(messages[^1].Content, reply, cancellationToken);
                return Results.Ok(new AssistantChatResponse(reply, contactChatId is not null, contactChatId));
            }
            catch (ContactChatQueueFullException)
            {
                return Results.Problem(
                    title: "The contact chat is unavailable.",
                    detail: "The contact chat queue cannot accept another conversation right now.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (HttpRequestException)
            {
                await notifier.NotifyAsync(messages[^1].Content, null, cancellationToken);
                return Results.Problem(
                    title: "The assistant is unavailable.",
                    detail: "The configured AI provider (Gemini or Ollama) could not be reached.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
            .AllowAnonymous()
            .RequireRateLimiting("assistant")
            .WithName("ChatWithAssistant")
            .WithSummary("Send a message to the portfolio assistant (Gemini on Azure, Ollama locally)")
            .Produces<AssistantChatResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static bool CanStartHumanChat(
        string? visitorName,
        IReadOnlyList<AssistantChatMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(visitorName) || visitorName.Length > 100)
        {
            return false;
        }

        var userMessages = messages
            .Where(message => message.Role == "user")
            .ToArray();
        return userMessages.Any(message =>
            message.Content.Contains(visitorName.Trim(), StringComparison.OrdinalIgnoreCase)) &&
            HasExplicitConsent(userMessages[^1].Content);
    }

    private static bool HasExplicitConsent(string message)
    {
        var normalized = message.Trim();
        var firstWord = normalized
            .Split([' ', ',', '.', '!', '?', ';', ':'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return (firstWord is not null && AffirmativeResponses.Contains(firstWord)) ||
            normalized.StartsWith("i agree", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("i consent", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("i accept", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateConversationSummary(IReadOnlyList<AssistantChatMessage> messages)
    {
        var summary = string.Join(
            "\n",
            messages.Where(message => message.Role == "user").Select(message => message.Content));
        return summary.Length > MaxSummaryLength
            ? summary[..MaxSummaryLength]
            : summary;
    }
}