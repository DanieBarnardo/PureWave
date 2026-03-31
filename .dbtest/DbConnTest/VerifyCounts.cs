using System.Globalization;
using MySqlConnector;
using Npgsql;

const string pg = "Server=purewavedb.postgres.database.azure.com;Database=postgres;Port=5432;User Id=danieadmpurewave;Password=Q!w2e3r4t5;Ssl Mode=Require;";
const string my = "Server=purewavedb.mysql.database.azure.com;Port=3306;UserID=danieadmpurewave;Password=Q!w2e3r4t5;Database=purewave;SslMode=Required;";
var tables = new[]{"clients","intake_submissions","projects","project_items","invoices","invoice_items","invoice_payments"};
await using var pgConn = new NpgsqlConnection(pg);
await using var myConn = new MySqlConnection(my);
await pgConn.OpenAsync();
await myConn.OpenAsync();
foreach (var table in tables)
{
    await using var pgCmd = new NpgsqlCommand($"select count(*) from {table};", pgConn);
    await using var myCmd = new MySqlCommand($"select count(*) from `{table}`;", myConn);
    var pgCount = Convert.ToInt64(await pgCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    var myCount = Convert.ToInt64(await myCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    Console.WriteLine($"{table}: postgres={pgCount}, mysql={myCount}");
}
