namespace PersonalPortfolio.Api.Notifications;

/// <summary>Reports delivery attempts without including notification content or credentials.</summary>
public sealed record SuggestionDeliveryResult(
    bool OwnerWasOnline,
    bool TelegramDelivered,
    bool OfflinePushAttempted,
    bool OfflinePushDelivered);