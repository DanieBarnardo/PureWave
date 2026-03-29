namespace PureWave.Admin.Models;

public sealed class InvoiceDetail
{
    public long Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateOnly InvoiceDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string ClientEmail { get; init; } = string.Empty;
    public string ClientPhone { get; init; } = string.Empty;
    public string ClientAddressBlock { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalDue { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal Outstanding { get; init; }
    public IReadOnlyList<InvoiceLineItem> Items { get; init; } = [];
    public IReadOnlyList<InvoicePaymentRecord> Payments { get; init; } = [];
}
