namespace PureWave.Admin.Models;

public sealed class ProjectReportRecord
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string PlanType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
