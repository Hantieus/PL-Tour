using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

[Table("Products")]
public class Product
{
    [Key]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Tên món ăn không được để trống")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Giá không được để trống")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [StringLength(200)]
    public string? ImageUrl { get; set; }

    public int VendorId { get; set; }

    public int? CategoryId { get; set; }

    public bool IsAvailable { get; set; } = true;

    public int StockQuantity { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }

    // Navigation properties
    [ForeignKey("VendorId")]
    public virtual Vendor? Vendor { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
}