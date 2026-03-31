using System.Globalization;
using MySqlConnector;
var sourceCs = "Server=purewavedb.mysql.database.azure.com;Port=3306;UserID=danieadmpurewave;Password=Q!w2e3r4t5;Database=purewave;SslMode=Required;";
var targetCs = "Server=mysqlssd2.zadns.co.za;Port=3307;UserID=danieadmpurewave;Password=Q!w2e3r4t5;Database=purewavedb;SslMode=Required;";
var tables = new[]{"clients","intake_submissions","projects","project_items","invoices","invoice_items","invoice_payments"};
await using var source = new MySqlConnection(sourceCs);
await using var target = new MySqlConnection(targetCs);
await source.OpenAsync();
await target.OpenAsync();
foreach (var table in tables)
{
    await using var sourceCmd = new MySqlCommand($"select count(*) from `{table}`;", source);
    await using var targetCmd = new MySqlCommand($"select count(*) from `{table}`;", target);
    var sourceCount = Convert.ToInt64(await sourceCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    var targetCount = Convert.ToInt64(await targetCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    Console.WriteLine($"{table}: source={sourceCount}, target={targetCount}");
}
