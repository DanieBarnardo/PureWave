using System.Text;
using System.Text.Json;
using PureWave.Web.Models;

namespace PureWave.Web.Services;

public sealed class FileIntakeSubmissionStore(IWebHostEnvironment environment) : IIntakeSubmissionStore
{
    private readonly string submissionsFolder = ResolveSubmissionsFolder(environment);

    public async Task<string> SaveAsync(IntakeQuestionnaire questionnaire, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(submissionsFolder);

        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        var safeName = MakeSafeFileName(questionnaire.FullName);
        var fileName = $"{stamp}-{safeName}.json";
        var fullPath = Path.Combine(submissionsFolder, fileName);

        var payload = new StoredIntakeSubmission
        {
            SubmittedAt = DateTimeOffset.Now,
            Questionnaire = questionnaire
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(fullPath, json, Encoding.UTF8, cancellationToken);
        return fileName;
    }

    public Task<IReadOnlyList<IntakeSubmissionFile>> ListAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(submissionsFolder);

        var files = new DirectoryInfo(submissionsFolder)
            .EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => new IntakeSubmissionFile
            {
                FileName = file.Name,
                FullPath = file.FullName,
                LastModified = new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero).ToLocalTime(),
                SizeBytes = file.Length
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<IntakeSubmissionFile>>(files);
    }

    public Task<(Stream Stream, string DownloadName)> OpenReadAsync(string fileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(submissionsFolder);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new FileNotFoundException("No intake file name was supplied.");
        }

        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(submissionsFolder, safeFileName);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The requested intake file was not found.", safeFileName);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult((stream, safeFileName));
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

    private static string ResolveSubmissionsFolder(IWebHostEnvironment environment)
    {
        const string azureDataPath = @"C:\home\data\StoredIntakes";

        if (OperatingSystem.IsWindows())
        {
            var homeDrive = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrWhiteSpace(homeDrive) &&
                homeDrive.Equals(@"C:\home", StringComparison.OrdinalIgnoreCase))
            {
                return azureDataPath;
            }
        }

        return Path.Combine(environment.ContentRootPath, "StoredIntakes");
    }

    private sealed class StoredIntakeSubmission
    {
        public DateTimeOffset SubmittedAt { get; init; }
        public required IntakeQuestionnaire Questionnaire { get; init; }
    }
}
