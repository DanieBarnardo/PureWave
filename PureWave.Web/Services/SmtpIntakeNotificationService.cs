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
        var firstName = q.FullName.Split(' ')[0];
        var subject = $"PureWave received your brief, {firstName}";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head><meta charset="UTF-8" /><meta name="viewport" content="width=device-width,initial-scale=1.0" /></head>
            <body style="margin:0;padding:32px 0;background:#e8e6e0;font-family:Arial,Helvetica,sans-serif;">

            <table width="620" align="center" cellpadding="0" cellspacing="0"
                   style="background:#0f0f0f;border-radius:4px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.18);">

              <!-- Banner -->
              <tr>
                <td style="padding:0;background:#0b0b0b;text-align:center;">
                  <img src="https://www.purewaveacoustic.co.za/images/purewave-banner.png"
                       width="620" alt="PureWave Home Theatre Consultancy"
                       style="display:block;width:100%;max-width:620px;" />
                </td>
              </tr>

              <!-- Gold rule -->
              <tr>
                <td style="height:3px;background:#c9a96e;font-size:0;line-height:0;">&nbsp;</td>
              </tr>

              <!-- Greeting -->
              <tr>
                <td style="padding:40px 48px 24px;">
                  <p style="margin:0 0 8px;font-size:11px;font-weight:bold;letter-spacing:0.15em;text-transform:uppercase;color:#c9a96e;">Brief received</p>
                  <h1 style="margin:0 0 20px;font-size:24px;font-weight:bold;line-height:1.3;color:#e8e4dc;font-family:Georgia,serif;">
                    Thanks for sharing your room, {{firstName}}. We have everything we need to get started.
                  </h1>
                  <p style="margin:0 0 14px;font-size:15px;line-height:1.75;color:rgba(232,228,220,0.68);">
                    Your brief has been received and will be reviewed shortly. There is no need to chase &mdash;
                    PureWave will be in touch via your preferred contact method once the brief has been read.
                  </p>
                  <p style="margin:0;font-size:15px;line-height:1.75;color:rgba(232,228,220,0.68);">
                    Here is a summary of what was captured:
                  </p>
                </td>
              </tr>

              <!-- Summary card -->
              <tr>
                <td style="padding:0 48px 32px;">
                  <table width="100%" cellpadding="0" cellspacing="0"
                         style="border:1px solid rgba(201,169,110,0.2);border-radius:6px;overflow:hidden;">
                    <tr>
                      <td style="padding:16px 20px;border-bottom:1px solid rgba(255,255,255,0.06);">
                        <span style="font-size:10px;font-weight:bold;letter-spacing:0.12em;text-transform:uppercase;color:rgba(232,228,220,0.35);">Plan</span><br/>
                        <span style="font-size:14px;font-weight:bold;color:#e8e4dc;">{{q.InterestedPlan}}</span>
                      </td>
                      <td style="padding:16px 20px;border-bottom:1px solid rgba(255,255,255,0.06);border-left:1px solid rgba(255,255,255,0.06);">
                        <span style="font-size:10px;font-weight:bold;letter-spacing:0.12em;text-transform:uppercase;color:rgba(232,228,220,0.35);">Service mode</span><br/>
                        <span style="font-size:14px;font-weight:bold;color:#e8e4dc;">{{q.ServiceMode}}</span>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:16px 20px;border-bottom:1px solid rgba(255,255,255,0.06);">
                        <span style="font-size:10px;font-weight:bold;letter-spacing:0.12em;text-transform:uppercase;color:rgba(232,228,220,0.35);">Project stage</span><br/>
                        <span style="font-size:14px;color:#e8e4dc;">{{q.ProjectStage}}</span>
                      </td>
                      <td style="padding:16px 20px;border-bottom:1px solid rgba(255,255,255,0.06);border-left:1px solid rgba(255,255,255,0.06);">
                        <span style="font-size:10px;font-weight:bold;letter-spacing:0.12em;text-transform:uppercase;color:rgba(232,228,220,0.35);">Budget band</span><br/>
                        <span style="font-size:14px;color:#e8e4dc;">{{q.BudgetBand}}</span>
                      </td>
                    </tr>
                    <tr>
                      <td colspan="2" style="padding:16px 20px;">
                        <span style="font-size:10px;font-weight:bold;letter-spacing:0.12em;text-transform:uppercase;color:rgba(232,228,220,0.35);">Contact preference</span><br/>
                        <span style="font-size:14px;color:#e8e4dc;">{{q.ContactPreference}}</span>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>

              <!-- What happens next -->
              <tr>
                <td style="padding:0 48px 8px;">
                  <p style="margin:0;font-size:11px;font-weight:bold;letter-spacing:0.15em;text-transform:uppercase;color:#c9a96e;">What happens next</p>
                </td>
              </tr>
              <tr>
                <td style="padding:16px 48px 8px;">
                  <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                      <td width="44" valign="top" style="padding:0 16px 24px 0;">
                        <div style="width:32px;height:32px;border-radius:50%;background:rgba(201,169,110,0.12);border:1px solid rgba(201,169,110,0.3);text-align:center;line-height:32px;font-size:12px;font-weight:bold;color:#c9a96e;">01</div>
                      </td>
                      <td valign="top" style="padding-bottom:24px;">
                        <p style="margin:0 0 4px;font-size:14px;font-weight:bold;color:#e8e4dc;">PureWave reviews your brief</p>
                        <p style="margin:0;font-size:13px;line-height:1.65;color:rgba(232,228,220,0.5);">Your brief is read and assessed — usually within 24 hours.</p>
                      </td>
                    </tr>
                    <tr>
                      <td width="44" valign="top" style="padding:0 16px 24px 0;">
                        <div style="width:32px;height:32px;border-radius:50%;background:rgba(201,169,110,0.12);border:1px solid rgba(201,169,110,0.3);text-align:center;line-height:32px;font-size:12px;font-weight:bold;color:#c9a96e;">02</div>
                      </td>
                      <td valign="top" style="padding-bottom:24px;">
                        <p style="margin:0 0 4px;font-size:14px;font-weight:bold;color:#e8e4dc;">You hear back via your preferred contact method</p>
                        <p style="margin:0;font-size:13px;line-height:1.65;color:rgba(232,228,220,0.5);">PureWave will reach out to discuss the right next step for your room.</p>
                      </td>
                    </tr>
                    <tr>
                      <td width="44" valign="top" style="padding:0 16px 8px 0;">
                        <div style="width:32px;height:32px;border-radius:50%;background:rgba(201,169,110,0.12);border:1px solid rgba(201,169,110,0.3);text-align:center;line-height:32px;font-size:12px;font-weight:bold;color:#c9a96e;">03</div>
                      </td>
                      <td valign="top" style="padding-bottom:8px;">
                        <p style="margin:0 0 4px;font-size:14px;font-weight:bold;color:#e8e4dc;">The consultation is arranged around you</p>
                        <p style="margin:0;font-size:13px;line-height:1.65;color:rgba(232,228,220,0.5);">The session is scheduled at a time that suits you, with the brief already in hand.</p>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>

              <!-- Reply note -->
              <tr>
                <td style="padding:8px 48px 40px;">
                  <p style="margin:0;font-size:13px;line-height:1.7;color:rgba(232,228,220,0.35);">
                    If you have anything to add before then, just reply to this email.
                  </p>
                </td>
              </tr>

              <!-- Divider -->
              <tr>
                <td style="height:1px;background:rgba(201,169,110,0.15);font-size:0;line-height:0;">&nbsp;</td>
              </tr>

              <!-- Footer -->
              <tr>
                <td style="padding:28px 48px;background:#080808;">
                  <p style="margin:0 0 2px;font-size:13px;color:rgba(232,228,220,0.5);">Regards,</p>
                  <p style="margin:0 0 1px;font-size:15px;font-weight:bold;color:#e8e4dc;">Danie Barnardo</p>
                  <p style="margin:0 0 14px;font-size:12px;color:rgba(201,169,110,0.65);">PureWave Home Theatre Consultancy</p>
                  <a href="https://www.purewaveacoustic.co.za"
                     style="font-size:12px;color:rgba(201,169,110,0.4);text-decoration:none;letter-spacing:0.05em;">
                    purewaveacoustic.co.za
                  </a>
                </td>
              </tr>

            </table>
            </body>
            </html>
            """;

        var message = new MailMessage(
            from: _settings.FromAddress,
            to: q.EmailAddress,
            subject: subject,
            body: html)
        {
            IsBodyHtml = true
        };

        return message;
    }
}
