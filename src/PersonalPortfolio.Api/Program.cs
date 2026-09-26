using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using PersonalPortfolio.Api.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
var telegramLogin = builder.Configuration.GetSection("TelegramLogin");
var ownerTelegramUserId = telegramLogin["OwnerUserId"];
var telegramLoginConfigured = !string.IsNullOrWhiteSpace(telegramLogin["ClientId"]) &&
                              !string.IsNullOrWhiteSpace(telegramLogin["ClientSecret"]) &&
                              !string.IsNullOrWhiteSpace(ownerTelegramUserId);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<AdminPresenceTracker>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminAuthentication.AdminPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(AdminAuthentication.AdminRole);
    });
});

var authentication = builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "PersonalPortfolio.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

if (telegramLoginConfigured)
{
    authentication.AddOpenIdConnect(AdminAuthentication.TelegramScheme, options =>
    {
        options.Authority = "https://oauth.telegram.org";
        options.ClientId = telegramLogin["ClientId"] ?? string.Empty;
        options.ClientSecret = telegramLogin["ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-telegram";
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("telegram:bot_access");

        options.Events.OnTokenValidated = context =>
        {
            var telegramUserId = context.Principal?.FindFirst("id")?.Value;
            if (string.IsNullOrWhiteSpace(ownerTelegramUserId) ||
                !string.Equals(telegramUserId, ownerTelegramUserId, StringComparison.Ordinal))
            {
                context.Fail("This Telegram account is not authorized as the site owner.");
                return Task.CompletedTask;
            }

            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, AdminAuthentication.AdminRole));
            }

            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/?adminLogin=failed");
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddSingleton<IAdminPresenceTracker>(
    services => services.GetRequiredService<AdminPresenceTracker>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/portfolio", () => new
{
    name = "Abel López",
    title = "Software Engineer",
    tagline = "I build practical digital products with modern frontend and backend tools.",
    focusAreas = new[]
    {
        "Full-stack product development",
        "Clean architecture",
        "Performance and UX",
        "API integration"
    },
    projects = new[]
    {
        new { name = "Cash Harmony", stack = new[] { "Angular", ".NET", "SQL" }, summary = "A financial operations platform focused on process clarity and business efficiency." },
        new { name = "Personal Portfolio", stack = new[] { "Angular", "ASP.NET Core", "Telegram" }, summary = "A personal site to showcase resume, projects and suggestions from visitors." },
        new { name = "Operational Dashboard", stack = new[] { "Angular", "REST API" }, summary = "A dashboard for operational visibility and business metrics." }
    }
});

app.MapControllers();
app.MapAdminEndpoints();
app.MapHub<AdminPresenceHub>("/hubs/admin")
    .RequireAuthorization(AdminAuthentication.AdminPolicy);

app.Run();
