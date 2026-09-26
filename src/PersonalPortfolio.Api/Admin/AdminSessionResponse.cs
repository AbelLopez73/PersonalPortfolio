namespace PersonalPortfolio.Api.Admin;

/// <summary>Describes the current admin login capability and session state.</summary>
public sealed record AdminSessionResponse(
    bool LoginEnabled,
    bool IsAdmin,
    string? DisplayName,
    bool IsConnected);