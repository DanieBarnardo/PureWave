using Microsoft.Extensions.Options;
using Npgsql;
using PureWave.Web.Models;

namespace PureWave.Web.Services;

public sealed class PostgresSchemaInitializer(
    IWebHostEnvironment environment,
    IOptions<PostgresSettings> settings,
    ILogger<PostgresSchemaInitializer> logger)
{
    private readonly string scriptPath = Path.Combine(environment.ContentRootPath, "Database", "001_create_intake_submissions.sql");
    private readonly string connectionString = settings.Value.ConnectionString;

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("PostgreSQL connection string is empty. Database initialization was skipped.");
            return;
        }

        if (!File.Exists(scriptPath))
        {
            logger.LogWarning("PostgreSQL schema script was not found at {ScriptPath}. Database initialization was skipped.", scriptPath);
            return;
        }

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation("PureWave intake database schema ensured.");
    }
}
