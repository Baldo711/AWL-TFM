namespace AccessWatchLite.Domain;

public sealed class AccessEvent
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserPrincipalName { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? ClientApp { get; set; }
    public string? ClientResource { get; set; }
    public string? AuthMethod { get; set; }
    public string? Status { get; set; }
    public string? ConditionalAccess { get; set; }
    public string? Error { get; set; }
    public string? Result { get; set; }
    public string? RiskLevel { get; set; }
    public string? RiskEventTypesJson { get; set; }
    public string? RawJson { get; set; }
    public bool IsIgnored { get; set; }
    public DateTime CreatedAt { get; set; }
}
