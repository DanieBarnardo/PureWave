namespace PureWave.Admin.Models;

public sealed class InvoiceLineItem
{
    public long Id { get; init; }
    public string ItemCategory { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string UnitLabel { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public decimal CostAmount { get; init; }
    public decimal LineTotal { get; init; }
}
