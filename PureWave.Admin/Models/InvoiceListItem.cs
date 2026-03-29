namespace PureWave.Admin.Models;

public sealed class InvoiceListItem
{
    public long Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public decimal TotalDue { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal Outstanding { get; init; }
    public string Status { get; init; } = string.Empty;
}
