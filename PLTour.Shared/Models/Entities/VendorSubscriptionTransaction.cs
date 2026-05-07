using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

[Table("VendorSubscriptionTransactions")]
public class VendorSubscriptionTransaction
{
    [Key]
    public int TransactionId { get; set; }

    [Required]
    public int VendorId { get; set; }

    [Required]
    [StringLength(30)]
    public string PlanCode { get; set; } = "Premium";

    [Required]
    public decimal Amount { get; set; }

    [StringLength(100)]
    public string Currency { get; set; } = "VND";

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    public string? OrderId { get; set; }

    [StringLength(100)]
    public string? TransactionNo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    [StringLength(2000)]
    public string? RawResponse { get; set; }

    [ForeignKey(nameof(VendorId))]
    public virtual Vendor Vendor { get; set; } = null!;
}
