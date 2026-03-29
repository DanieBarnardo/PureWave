using System.ComponentModel.DataAnnotations;

namespace PureWave.Admin.Models;

public sealed class InvoicePaymentRecord
{
    public long Id { get; init; }
    public long InvoiceId { get; set; }
    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Range(0.01, 100000000)]
    public decimal Amount { get; set; }

    [StringLength(120)]
    public string Reference { get; set; } = string.Empty;

    [StringLength(800)]
    public string Notes { get; set; } = string.Empty;
}
