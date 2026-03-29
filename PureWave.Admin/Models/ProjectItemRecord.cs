using System.ComponentModel.DataAnnotations;

namespace PureWave.Admin.Models;

public sealed class ProjectItemRecord
{
    public long Id { get; set; }
    public long ProjectId { get; set; }

    [Required, StringLength(32)]
    public string ItemCategory { get; set; } = "Consultancy";

    [Required, StringLength(400)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Quantity { get; set; } = 1;

    [StringLength(32)]
    public string UnitLabel { get; set; } = "Hr";

    [Range(0, 100000000)]
    public decimal CostAmount { get; set; }

    [Range(0, 100000000)]
    public decimal BillableAmount { get; set; }

    public DateOnly IncurredOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(800)]
    public string Notes { get; set; } = string.Empty;
}
