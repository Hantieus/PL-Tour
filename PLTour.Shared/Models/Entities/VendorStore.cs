using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

[Table("VendorStores")]
public class VendorStore
{
    [Key]
    public int StoreId { get; set; }

    [Required]
    public int VendorId { get; set; }

    public int? LocationId { get; set; }

    [Required]
    [StringLength(200)]
    public string StoreName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [StringLength(30)]
    public string Plan { get; set; } = "Free";

    public DateTime? PlanExpiresAt { get; set; }

    public bool IsDefault { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    [ForeignKey(nameof(VendorId))]
    public virtual Vendor Vendor { get; set; } = null!;

    [ForeignKey(nameof(LocationId))]
    public virtual Location? Location { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
