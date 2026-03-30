using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using PureWave.Web.Models;

namespace PureWave.Web.Services;

public sealed class PostgresIntakeSubmissionStore(IOptions<PostgresSettings> settings) : IIntakeSubmissionStore
{
    private readonly string connectionString = settings.Value.ConnectionString;

    public async Task<string> SaveAsync(IntakeQuestionnaire questionnaire, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            insert into intake_submissions
            (
                submitted_at,
                full_name,
                email_address,
                phone_number,
                suburb_or_area,
                project_stage,
                interested_plan,
                service_mode,
                service_format,
                room_type,
                primary_goals,
                room_dimensions,
                budget_band,
                timeline,
                needs_acoustic_design,
                needs_calibration,
                needs_automation,
                needs_procurement_advice,
                needs_existing_equipment_installation,
                needs_guidance_only,
                existing_equipment,
                key_challenges,
                contact_preference
            )
            values
            (
                @submitted_at,
                @full_name,
                @email_address,
                @phone_number,
                @suburb_or_area,
                @project_stage,
                @interested_plan,
                @service_mode,
                @service_format,
                @room_type,
                @primary_goals,
                @room_dimensions,
                @budget_band,
                @timeline,
                @needs_acoustic_design,
                @needs_calibration,
                @needs_automation,
                @needs_procurement_advice,
                @needs_existing_equipment_installation,
                @needs_guidance_only,
                @existing_equipment,
                @key_challenges,
                @contact_preference
            )
            returning id;
            """;

        var submittedAt = DateTimeOffset.UtcNow;

        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, questionnaire, submittedAt);

        var id = (long)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("Database did not return a submission id."));

        return BuildDownloadName(id, questionnaire.FullName, submittedAt);
    }

    public async Task<IReadOnlyList<IntakeSubmissionFile>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            select id, submitted_at, full_name
            from intake_submissions
            order by submitted_at desc, id desc;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<IntakeSubmissionFile>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            var submittedAt = reader.GetFieldValue<DateTimeOffset>(1).ToLocalTime();
            var fullName = reader.GetString(2);
            var fileName = BuildDownloadName(id, fullName, submittedAt);

            items.Add(new IntakeSubmissionFile
            {
                FileName = fileName,
                LastModified = submittedAt,
                SizeBytes = 0
            });
        }

        return items;
    }

    public async Task<(Stream Stream, string DownloadName)> OpenReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var id = ParseId(fileName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            select
                id,
                submitted_at,
                full_name,
                email_address,
                phone_number,
                suburb_or_area,
                project_stage,
                interested_plan,
                service_mode,
                service_format,
                room_type,
                primary_goals,
                room_dimensions,
                budget_band,
                timeline,
                needs_acoustic_design,
                needs_calibration,
                needs_automation,
                needs_procurement_advice,
                needs_existing_equipment_installation,
                needs_guidance_only,
                existing_equipment,
                key_challenges,
                contact_preference
            from intake_submissions
            where id = @id;
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new FileNotFoundException("The requested intake submission was not found.", fileName);
        }

        var submission = new DatabaseIntakeSubmission
        {
            Id = reader.GetInt64(0),
            SubmittedAt = reader.GetFieldValue<DateTimeOffset>(1),
            Questionnaire = new IntakeQuestionnaire
            {
                FullName = reader.GetString(2),
                EmailAddress = reader.GetString(3),
                PhoneNumber = reader.GetString(4),
                SuburbOrArea = reader.GetString(5),
                ProjectStage = reader.GetString(6),
                InterestedPlan = reader.GetString(7),
                ServiceMode = reader.GetString(8),
                ServiceFormat = reader.GetString(9),
                RoomType = reader.GetString(10),
                PrimaryGoals = reader.GetString(11),
                RoomDimensions = reader.GetString(12),
                BudgetBand = reader.GetString(13),
                Timeline = reader.GetString(14),
                NeedsAcousticDesign = reader.GetBoolean(15),
                NeedsCalibration = reader.GetBoolean(16),
                NeedsAutomation = reader.GetBoolean(17),
                NeedsProcurementAdvice = reader.GetBoolean(18),
                NeedsExistingEquipmentInstallation = reader.GetBoolean(19),
                NeedsGuidanceOnly = reader.GetBoolean(20),
                ExistingEquipment = reader.GetString(21),
                KeyChallenges = reader.GetString(22),
                ContactPreference = reader.GetString(23)
            }
        };

        var json = JsonSerializer.Serialize(submission, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        return (new MemoryStream(bytes), BuildDownloadName(submission.Id, submission.Questionnaire.FullName, submission.SubmittedAt));
    }

    private static void AddParameters(NpgsqlCommand command, IntakeQuestionnaire questionnaire, DateTimeOffset submittedAt)
    {
        command.Parameters.AddWithValue("submitted_at", submittedAt);
        command.Parameters.AddWithValue("full_name", questionnaire.FullName);
        command.Parameters.AddWithValue("email_address", questionnaire.EmailAddress);
        command.Parameters.AddWithValue("phone_number", questionnaire.PhoneNumber ?? string.Empty);
        command.Parameters.AddWithValue("suburb_or_area", questionnaire.SuburbOrArea);
        command.Parameters.AddWithValue("project_stage", questionnaire.ProjectStage);
        command.Parameters.AddWithValue("interested_plan", questionnaire.InterestedPlan);
        command.Parameters.AddWithValue("service_mode", questionnaire.ServiceMode);
        command.Parameters.AddWithValue("service_format", questionnaire.ServiceFormat);
        command.Parameters.AddWithValue("room_type", questionnaire.RoomType);
        command.Parameters.AddWithValue("primary_goals", questionnaire.PrimaryGoals);
        command.Parameters.AddWithValue("room_dimensions", questionnaire.RoomDimensions);
        command.Parameters.AddWithValue("budget_band", questionnaire.BudgetBand);
        command.Parameters.AddWithValue("timeline", questionnaire.Timeline);
        command.Parameters.AddWithValue("needs_acoustic_design", questionnaire.NeedsAcousticDesign);
        command.Parameters.AddWithValue("needs_calibration", questionnaire.NeedsCalibration);
        command.Parameters.AddWithValue("needs_automation", questionnaire.NeedsAutomation);
        command.Parameters.AddWithValue("needs_procurement_advice", questionnaire.NeedsProcurementAdvice);
        command.Parameters.AddWithValue("needs_existing_equipment_installation", questionnaire.NeedsExistingEquipmentInstallation);
        command.Parameters.AddWithValue("needs_guidance_only", questionnaire.NeedsGuidanceOnly);
        command.Parameters.AddWithValue("existing_equipment", questionnaire.ExistingEquipment ?? string.Empty);
        command.Parameters.AddWithValue("key_challenges", questionnaire.KeyChallenges ?? string.Empty);
        command.Parameters.AddWithValue("contact_preference", questionnaire.ContactPreference);
    }

    private static string BuildDownloadName(long id, string fullName, DateTimeOffset submittedAt)
    {
        var safeName = MakeSafeFileName(fullName);
        return $"{submittedAt:yyyyMMdd-HHmmss}-{id:D6}-{safeName}.json";
    }

    private static long ParseId(string fileName)
    {
        var parts = Path.GetFileNameWithoutExtension(fileName).Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !long.TryParse(parts[2], out var id))
        {
            throw new FileNotFoundException("The requested intake submission name is invalid.", fileName);
        }

        return id;
    }

    private static string MakeSafeFileName(string? value)
    {
        var fallback = "intake";
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim())
        {
            builder.Append(invalid.Contains(character) ? '-' : character);
        }

        var cleaned = builder.ToString().Replace(' ', '-');
        return string.IsNullOrWhiteSpace(cleaned) ? fallback : cleaned;
    }
}
