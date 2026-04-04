using Microsoft.Extensions.Options;
using MySqlConnector;
using PureWave.Admin.Models;

namespace PureWave.Admin.Services;

public sealed class AdminSchemaInitializer(
    IWebHostEnvironment environment,
    IOptions<MySqlSettings> settings,
    ILogger<AdminSchemaInitializer> logger)
{
    private readonly string connectionString = settings.Value.ConnectionString;
    private readonly string scriptPath = Path.Combine(environment.ContentRootPath, "Database", "001_create_admin_schema.sql");

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Admin app MySQL connection string is empty. Schema initialization skipped.");
            return;
        }

        if (!File.Exists(scriptPath))
        {
            logger.LogWarning("Admin schema script was not found at {ScriptPath}. Schema initialization skipped.", scriptPath);
            return;
        }

        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = await File.ReadAllTextAsync(scriptPath, cancellationToken);
        var statements = sql.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => !string.IsNullOrWhiteSpace(statement))
            .ToArray();

        foreach (var statement in statements)
        {
            await using var command = new MySqlCommand(statement, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await EnsureColumnAsync(connection, "intake_submissions", "service_mode", "varchar(120) not null default ''", cancellationToken);
        await EnsureColumnAsync(connection, "intake_submissions", "service_format", "varchar(120) not null default ''", cancellationToken);
        await EnsureColumnAsync(connection, "intake_submissions", "client_id", "bigint null", cancellationToken);
        await EnsureColumnAsync(connection, "projects", "plan_type", "varchar(60) not null default ''", cancellationToken);
    }

    private static async Task EnsureColumnAsync(
        MySqlConnection connection,
        string tableName,
        string columnName,
        string columnDefinition,
        CancellationToken cancellationToken)
    {
        await using var existsCommand = new MySqlCommand(
            """
            select count(*)
            from information_schema.columns
            where table_schema = database()
              and table_name = @table_name
              and column_name = @column_name;
            """,
            connection);
        existsCommand.Parameters.AddWithValue("@table_name", tableName);
        existsCommand.Parameters.AddWithValue("@column_name", columnName);

        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken)) > 0;
        if (exists)
        {
            return;
        }

        await using var alterCommand = new MySqlCommand(
            $"alter table `{tableName}` add column `{columnName}` {columnDefinition};",
            connection);
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
