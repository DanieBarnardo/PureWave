namespace PureWave.Web.Models;

public sealed class PostgresSettings
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; set; } = string.Empty;
}
