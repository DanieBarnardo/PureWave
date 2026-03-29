using System.ComponentModel.DataAnnotations;

namespace PureWave.Admin.Models;

public sealed class ProjectRecord
{
    public long Id { get; set; }

    [Required]
    public long ClientId { get; set; }

    [Required, StringLength(140)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Status { get; set; } = "Active";

    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }

    public int InvoiceCount { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public long? LatestInvoiceId { get; set; }
    public string LatestInvoiceNumber { get; set; } = string.Empty;
    public string LatestInvoiceStatus { get; set; } = string.Empty;
}
