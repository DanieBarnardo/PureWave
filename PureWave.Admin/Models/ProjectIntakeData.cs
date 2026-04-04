namespace PureWave.Admin.Models;

public sealed class ProjectIntakeData
{
    public string ClientFullName { get; init; } = string.Empty;
    public string SuburbOrArea { get; init; } = string.Empty;
    public string ProjectStage { get; init; } = string.Empty;
    public string BudgetBand { get; init; } = string.Empty;
    public string Timeline { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public string RoomDimensions { get; init; } = string.Empty;
    public string PrimaryGoals { get; init; } = string.Empty;
    public string ExistingEquipment { get; init; } = string.Empty;
    public string KeyChallenges { get; init; } = string.Empty;
    public bool NeedsAcousticDesign { get; init; }
    public bool NeedsCalibration { get; init; }
    public bool NeedsAutomation { get; init; }
}
