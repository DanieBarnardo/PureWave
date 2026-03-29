namespace PureWave.Admin.Models;

public sealed class AdminAuthSettings
{
    public const string SectionName = "AdminAuth";

    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
}
