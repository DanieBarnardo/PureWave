namespace PureWave.Web.Models;

public sealed class DatabaseIntakeSubmission
{
    public long Id { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public required IntakeQuestionnaire Questionnaire { get; init; }
}
