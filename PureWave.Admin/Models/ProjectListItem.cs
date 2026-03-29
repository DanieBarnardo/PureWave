namespace PureWave.Admin.Models;

public sealed class ProjectListItem
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalCost { get; init; }
    public decimal TotalBillable { get; init; }
}
