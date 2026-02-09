namespace AccessWatchLite.Domain;

public sealed class SimMetadata
{
    public int Id { get; set; }
    public bool HasData { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public int TotalEvents { get; set; }
    public int UnanalyzedEvents { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
