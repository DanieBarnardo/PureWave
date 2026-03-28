namespace PureWave.Web.Models;

public sealed class ServicePlan
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Tagline { get; init; }
    public required string BestFor { get; init; }
    public required string PricingModel { get; init; }
    public required string Turnaround { get; init; }
    public required string AccentLabel { get; init; }
    public required IReadOnlyList<string> Deliverables { get; init; }
    public required IReadOnlyList<string> Outcomes { get; init; }
}
