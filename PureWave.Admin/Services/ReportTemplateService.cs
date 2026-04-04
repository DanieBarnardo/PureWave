using PureWave.Admin.Models;

namespace PureWave.Admin.Services;

public sealed class ReportTemplateService(IWebHostEnvironment environment, IConfiguration configuration)
{
    private static readonly Dictionary<string, string> TemplateFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["blueprint"] = "Report_Template_-_The_Blueprint.md",
        ["tuning"] = "Report_Template_-_The_Tuning.md",
        ["director"] = "Report_Template_-_The_Director.md",
        ["virtual-blueprint"] = "Report_Template_-_The_Virtual_Blueprint.md",
        ["procurement-report"] = "Report_Template_-_The_Procurement_Report.md"
    };

    public static IReadOnlyList<(string Slug, string Name)> PlanTypes { get; } =
    [
        ("blueprint", "The Blueprint"),
        ("tuning", "The Tuning"),
        ("director", "The Director"),
        ("virtual-blueprint", "The Virtual Blueprint"),
        ("procurement-report", "The Procurement Report")
    ];

    public string GenerateReport(string planType, string clientName, ProjectIntakeData? intake)
    {
        if (!TemplateFiles.TryGetValue(planType, out var filename))
            return $"# No template found for plan type: {planType}";

        var templateDir = ResolveTemplateDirectory();
        var templatePath = Path.Combine(templateDir, filename);

        if (!File.Exists(templatePath))
            return $"# Template file not found\n\nExpected at: `{templatePath}`";

        var content = File.ReadAllText(templatePath);
        content = FixHeaderLineBreaks(content);
        return ApplySubstitutions(content, clientName, intake);
    }

    private string ResolveTemplateDirectory()
    {
        var configured = configuration["TemplatePath"];
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));

        return Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "Documents", "Templates"));
    }

    // Adds two trailing spaces to bold field lines (e.g. **Client:** ...) so markdown renders them as <br>
    public static string FixHeaderLineBreaks(string content)
    {
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd();
            if (trimmed.StartsWith("**") && trimmed.Contains(":**") && !trimmed.EndsWith("  "))
                lines[i] = trimmed + "  ";
        }
        return string.Join('\n', lines);
    }

    private static string ApplySubstitutions(string content, string clientName, ProjectIntakeData? intake)
    {
        var date = DateTime.Today.ToString("d MMMM yyyy");

        content = content.Replace("[Client Name]", clientName);
        content = content.Replace("[Date]", date);

        if (intake is null)
            return content;

        // Client Brief — Blueprint & Tuning
        content = content.Replace("[e.g., Pre-build / Renovation / Equipment planning]", intake.ProjectStage);
        content = content.Replace("[e.g., R50,000 – R80,000 / To be confirmed]", intake.BudgetBand);
        content = content.Replace("[e.g., 3 months / Flexible]", intake.Timeline);

        // Room dimensions — Blueprint
        content = content.Replace("[Length]m x [Width]m x [Height]m", intake.RoomDimensions);

        // Executive summary goal — Blueprint
        if (!string.IsNullOrWhiteSpace(intake.PrimaryGoals))
        {
            var shortGoal = FirstSentence(intake.PrimaryGoals);
            content = content.Replace(
                "[brief goal, e.g., a new dedicated cinema room / a home theatre upgrade in the lounge]",
                shortGoal);
            // Director — Project Goal
            content = content.Replace(
                "[e.g., A dedicated 7.2.4 Dolby Atmos cinema room with full Home Assistant integration and a 130\" acoustically transparent screen]",
                intake.PrimaryGoals);
        }

        // Director — property location
        if (!string.IsNullOrWhiteSpace(intake.SuburbOrArea))
            content = content.Replace("[Property / Location]", intake.SuburbOrArea);

        // Tuning — pre-existing client issues
        if (!string.IsNullOrWhiteSpace(intake.KeyChallenges))
        {
            var challenges = FormatBullets(intake.KeyChallenges);
            content = content.Replace(
                "[Summarise what the client described before the session — e.g., boomy bass, dialogue intelligibility problems, weak surround presence, inconsistent volume between content types, etc.]",
                intake.KeyChallenges);
            content = content.Replace("- [Issue 1]\n- [Issue 2]\n- [Issue 3]", challenges);
        }

        // Tuning — existing equipment
        if (!string.IsNullOrWhiteSpace(intake.ExistingEquipment))
        {
            // Replace the first generic equipment placeholder in the System Overview
            content = content.Replace(
                "| **AV Receiver / Processor** | [Brand / Model] | [e.g., Running Audyssey MultEQ XT32] |",
                $"| **Existing Equipment** | {intake.ExistingEquipment} | — |");
        }

        return content;
    }

    private static string FirstSentence(string text)
    {
        var end = text.IndexOfAny(['.', '!', '?']);
        return end > 0 ? text[..(end + 1)].Trim() : text.Length > 120 ? text[..120].Trim() + "..." : text.Trim();
    }

    private static string FormatBullets(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join("\n", lines.Select(l => l.StartsWith('-') ? l : $"- {l}"));
    }
}
