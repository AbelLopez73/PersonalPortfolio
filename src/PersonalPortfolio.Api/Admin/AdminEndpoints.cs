using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PersonalPortfolio.Api.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/session", (
            HttpContext context,
            IConfiguration configuration,
            IAdminPresenceTracker presence) =>
        {
            var isAdmin = context.User.Identity?.IsAuthenticated == true &&
                          context.User.IsInRole(AdminAuthentication.AdminRole);
            var loginEnabled = !string.IsNullOrWhiteSpace(configuration["TelegramLogin:ClientId"]) &&
                               !string.IsNullOrWhiteSpace(configuration["TelegramLogin:ClientSecret"]) &&
                               !string.IsNullOrWhiteSpace(configuration["TelegramLogin:OwnerUserId"]);

            return TypedResults.Ok(new AdminSessionResponse(
                loginEnabled,
                isAdmin,
                isAdmin ? context.User.FindFirst("name")?.Value : null,
                isAdmin && presence.IsOwnerOnline));
        })
            .AllowAnonymous()
            .WithName("GetAdminSession")
            .WithSummary("Get the current admin session state")
            .Produces<AdminSessionResponse>(StatusCodes.Status200OK);

        app.MapGet("/api/admin/login", (
            IConfiguration configuration,
            HttpContext context) =>
        {
            var loginEnabled = !string.IsNullOrWhiteSpace(configuration["TelegramLogin:ClientId"]) &&
                               !string.IsNullOrWhiteSpace(configuration["TelegramLogin:ClientSecret"]) &&
                               !string.IsNullOrWhiteSpace(configuration["TelegramLogin:OwnerUserId"]);

            if (!loginEnabled)
            {
                return TypedResults.Problem(
                    title: "Telegram admin login is not configured.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/" },
                [AdminAuthentication.TelegramScheme]);
        })
            .AllowAnonymous()
            .WithName("StartTelegramAdminLogin")
            .WithSummary("Start the Telegram OpenID Connect login flow")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapPost("/api/admin/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return TypedResults.NoContent();
        })
            .RequireAuthorization(AdminAuthentication.AdminPolicy)
            .WithName("LogoutAdmin")
            .WithSummary("End the current admin session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}