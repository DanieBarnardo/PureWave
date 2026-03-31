namespace PureWave.Admin.Models;

public sealed class MySqlSettings
{
    public const string SectionName = "MySql";

    public string ConnectionString { get; set; } = string.Empty;
}
