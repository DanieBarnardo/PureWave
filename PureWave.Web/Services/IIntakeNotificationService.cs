using PureWave.Web.Models;

namespace PureWave.Web.Services;

public interface IIntakeNotificationService
{
    Task NotifyAsync(IntakeQuestionnaire questionnaire, CancellationToken cancellationToken = default);
}
