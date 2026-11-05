namespace PersonalPortfolio.Api.Notifications;

/// <summary>Contains the full suggestion delivered to the owner's private Telegram chat.</summary>
public sealed record SuggestionNotification(string Name, string Email, string Message);