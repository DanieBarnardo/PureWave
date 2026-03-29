namespace PureWave.Web.Models;

public sealed class IntakeSubmissionFile
{
    public required string FileName { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public long SizeBytes { get; init; }
}
