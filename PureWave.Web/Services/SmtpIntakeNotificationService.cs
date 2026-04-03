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

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
            EnableSsl = false
        };

        // Internal notification to intake@purewaveacoustic.co.za
        try
        {
            using var notification = BuildNotification(questionnaire);
            await client.SendMailAsync(notification, cancellationToken);
            logger.LogInformation("Intake notification sent for {FullName} ({Email}).", questionnaire.FullName, questionnaire.EmailAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send intake notification for {FullName} ({Email}).", questionnaire.FullName, questionnaire.EmailAddress);
        }

        // Confirmation to the customer
        if (!string.IsNullOrWhiteSpace(questionnaire.EmailAddress))
        {
            try
            {
                using var confirmation = BuildConfirmation(questionnaire);
                await client.SendMailAsync(confirmation, cancellationToken);
                logger.LogInformation("Intake confirmation sent to {Email}.", questionnaire.EmailAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send intake confirmation to {Email}.", questionnaire.EmailAddress);
            }
        }
    }

    private MailMessage BuildNotification(IntakeQuestionnaire q)
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

        var message = new MailMessage(
            from: _settings.FromAddress,
            to: _settings.NotificationAddress,
            subject: subject,
            body: body.ToString());

        message.ReplyToList.Add(new MailAddress(q.EmailAddress, q.FullName));

        return message;
    }

    private MailMessage BuildConfirmation(IntakeQuestionnaire q)
    {
        var subject = $"PureWave received your brief, {q.FullName.Split(' ')[0]}";

        var body = new StringBuilder();
        body.AppendLine($"Hi {q.FullName.Split(' ')[0]},");
        body.AppendLine();
        body.AppendLine("Your intake brief has been received. Here is a summary of what was captured:");
        body.AppendLine();
        body.AppendLine($"  Plan:            {q.InterestedPlan}");
        body.AppendLine($"  Service mode:    {q.ServiceMode}");
        body.AppendLine($"  Project stage:   {q.ProjectStage}");
        body.AppendLine($"  Budget band:     {q.BudgetBand}");
        body.AppendLine($"  Timeline:        {q.Timeline}");
        body.AppendLine($"  Contact via:     {q.ContactPreference}");
        body.AppendLine();
        body.AppendLine("What happens next:");
        body.AppendLine();
        body.AppendLine("  1. PureWave reviews your brief — usually within 24 hours.");
        body.AppendLine("  2. You will hear back via your preferred contact method.");
        body.AppendLine("  3. A consultation slot is arranged at a time that suits you.");
        body.AppendLine();
        body.AppendLine("If you have anything to add in the meantime, just reply to this email.");
        body.AppendLine();
        body.AppendLine("Danie Barnardo");
        body.AppendLine("PureWave Home Theatre Consultancy");
        body.AppendLine("purewaveacoustic.co.za");

        return new MailMessage(
            from: _settings.FromAddress,
            to: q.EmailAddress,
            subject: subject,
            body: body.ToString());
    }
}
