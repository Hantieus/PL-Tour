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
    public string ShopName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string OwnerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(500)]
    public string Address { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string LogoUrl { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public bool IsActive { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string Notes { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; } = default!;

    // THÊM DÒNG NÀY: Collection products
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}