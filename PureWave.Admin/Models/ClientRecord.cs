using System.ComponentModel.DataAnnotations;

namespace PureWave.Admin.Models;

public sealed class ClientRecord
{
    public long Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress, StringLength(256)]
    public string EmailAddress { get; set; } = string.Empty;

    [StringLength(64)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(160)]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(160)]
    public string AddressLine2 { get; set; } = string.Empty;

    [StringLength(80)]
    public string Suburb { get; set; } = string.Empty;

    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [StringLength(1200)]
    public string Notes { get; set; } = string.Empty;
}
