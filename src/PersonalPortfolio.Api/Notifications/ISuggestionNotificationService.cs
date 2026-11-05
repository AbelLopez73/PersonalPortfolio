namespace PersonalPortfolio.Api.Notifications;

public interface ISuggestionNotificationService
{
    Task<SuggestionDeliveryResult> NotifyOwnerAsync(
        SuggestionNotification suggestion,
        CancellationToken cancellationToken);
}