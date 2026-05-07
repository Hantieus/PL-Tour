using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

[Table("Vendors")]
public class Vendor
{
    [Key]
    public int VendorId { get; set; }

    [Required]
    [StringLength(200)]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Address { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public bool IsActive { get; set; } = false;

    [StringLength(30)]
    public string Plan { get; set; } = "Free";

    public DateTime? PlanExpiresAt { get; set; }

    [StringLength(200)]
    public string? AvatarUrl { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public virtual ICollection<VendorStore> Stores { get; set; } = new List<VendorStore>();
}
