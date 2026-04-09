using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

[Table("Products")]
public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [StringLength(200)]
    public string ImageUrl { get; set; }

    public int VendorId { get; set; }

    public int? CategoryId { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int StockQuantity { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }

    // Navigation properties
    [ForeignKey("VendorId")]
    public virtual Vendor Vendor { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category Category { get; set; }
}