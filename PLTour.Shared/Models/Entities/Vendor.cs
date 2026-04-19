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
    public string ShopName { get; set; }

    [Required]
    [StringLength(100)]
    public string OwnerName { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(20)]
    public string Phone { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(500)]
    public string Address { get; set; }

    public int? CategoryId { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(500)]
    public string LogoUrl { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public bool IsActive { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string Notes { get; set; }

    // Navigation properties
    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; }

    // THÊM DÒNG NÀY: Collection products
    public virtual ICollection<Product> Products { get; set; }
}