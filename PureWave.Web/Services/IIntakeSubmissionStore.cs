using PureWave.Web.Models;

namespace PureWave.Web.Services;

public interface IIntakeSubmissionStore
{
    Task<string> SaveAsync(IntakeQuestionnaire questionnaire, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IntakeSubmissionFile>> ListAsync(CancellationToken cancellationToken = default);
    Task<(Stream Stream, string DownloadName)> OpenReadAsync(string fileName, CancellationToken cancellationToken = default);
}
