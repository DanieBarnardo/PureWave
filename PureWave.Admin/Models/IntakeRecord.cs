namespace PureWave.Admin.Models;

public sealed class IntakeRecord
{
    public long Id { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string SuburbOrArea { get; init; } = string.Empty;
    public string InterestedPlan { get; init; } = string.Empty;
    public string BudgetBand { get; init; } = string.Empty;
    public string ProjectStage { get; init; } = string.Empty;
    public string PrimaryGoals { get; init; } = string.Empty;
    public long? ClientId { get; init; }
}
