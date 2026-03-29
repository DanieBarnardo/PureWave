using Microsoft.Extensions.Options;
using Npgsql;
using PureWave.Admin.Models;

namespace PureWave.Admin.Services;

public sealed class AdminSchemaInitializer(
    IWebHostEnvironment environment,
    IOptions<PostgresSettings> settings,
    ILogger<AdminSchemaInitializer> logger)
{
    private readonly string connectionString = settings.Value.ConnectionString;
    private readonly string scriptPath = Path.Combine(environment.ContentRootPath, "Database", "001_create_admin_schema.sql");

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Admin app PostgreSQL connection string is empty. Schema initialization skipped.");
            return;
        }

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
