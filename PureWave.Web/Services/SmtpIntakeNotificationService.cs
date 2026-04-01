using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using PureWave.Web.Models;

namespace PureWave.Web.Services;

public sealed class SmtpIntakeNotificationService(
    IOptions<SmtpSettings> settings,
    ILogger<SmtpIntakeNotificationService> logger) : IIntakeNotificationService
{
    private readonly SmtpSettings _settings = settings.Value;

    public async Task NotifyAsync(IntakeQuestionnaire questionnaire, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.NotificationAddress))
        {
            logger.LogWarning("Intake notification skipped — SMTP host or notification address is not configured.");
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = false
            };

            using var message = BuildMessage(questionnaire);
            await client.SendMailAsync(message, cancellationToken);

            logger.LogInformation("Intake notification sent for {FullName} ({Email}).", questionnaire.FullName, questionnaire.EmailAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send intake notification for {FullName} ({Email}).", questionnaire.FullName, questionnaire.EmailAddress);
        }
    }

    private MailMessage BuildMessage(IntakeQuestionnaire q)
    {
        var subject = $"New intake: {q.FullName} — {q.InterestedPlan} ({q.ServiceMode})";

        var body = new StringBuilder();
        body.AppendLine("A new intake questionnaire has been submitted on PureWave.");
        body.AppendLine();
        body.AppendLine("--- Contact ---");
        body.AppendLine($"Name:            {q.FullName}");
        body.AppendLine($"Email:           {q.EmailAddress}");
        body.AppendLine($"Phone:           {q.PhoneNumber}");
        body.AppendLine($"Area:            {q.SuburbOrArea}");
        body.AppendLine($"Contact via:     {q.ContactPreference}");
        body.AppendLine();
        body.AppendLine("--- Project ---");
        body.AppendLine($"Service mode:    {q.ServiceMode}");
        body.AppendLine($"Service format:  {q.ServiceFormat}");
        body.AppendLine($"Plan interest:   {q.InterestedPlan}");
        body.AppendLine($"Project stage:   {q.ProjectStage}");
        body.AppendLine($"Room type:       {q.RoomType}");
        body.AppendLine($"Budget band:     {q.BudgetBand}");
        body.AppendLine($"Timeline:        {q.Timeline}");
        body.AppendLine();
        body.AppendLine("--- Services of interest ---");
        if (q.NeedsAcousticDesign) body.AppendLine("  - Acoustic design / remote acoustic and layout strategy");
        if (q.NeedsCalibration) body.AppendLine("  - Calibration and DSP tuning");
        if (q.NeedsAutomation) body.AppendLine("  - Automation and control design");
        if (q.NeedsProcurementAdvice) body.AppendLine("  - Independent procurement advice");
        if (q.NeedsExistingEquipmentInstallation) body.AppendLine("  - Installation of existing equipment");
        if (q.NeedsGuidanceOnly) body.AppendLine("  - Guidance and advice only");
        body.AppendLine();
        body.AppendLine("--- Technical brief ---");
        body.AppendLine($"Goals:\n{q.PrimaryGoals}");
        body.AppendLine();
        body.AppendLine($"Room dimensions:\n{q.RoomDimensions}");
        if (!string.IsNullOrWhiteSpace(q.ExistingEquipment))
        {
            body.AppendLine();
            body.AppendLine($"Existing equipment:\n{q.ExistingEquipment}");
        }
        if (!string.IsNullOrWhiteSpace(q.KeyChallenges))
        {
            body.AppendLine();
            body.AppendLine($"Key challenges:\n{q.KeyChallenges}");
        }

        return new MailMessage(
            from: _settings.FromAddress,
            to: _settings.NotificationAddress,
            subject: subject,
            body: body.ToString());
    }
}
