using System.ComponentModel.DataAnnotations;

namespace PLTour.Shared.Models.Entities;

public class Location
{
    [Key]
    public int LocationId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; } // Mô tả ngắn (có thể dùng chung)

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    public int CategoryId { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    public int OrderIndex { get; set; }

    [Range(10, 500, ErrorMessage = "Bán kính phải từ 10 đến 500 mét")]
    [Display(Name = "Bán kính kích hoạt (mét)")]
    public int Radius { get; set; } = 50;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }
    
    [StringLength(100)]
    public string? QrCode { get; set; }

    public DateTime? QrCodeGeneratedAt { get; set; } = DateTime.UtcNow; //TG tạo QR

    // Navigation properties
    public virtual Category? Category { get; set; }
    public virtual ICollection<Narration>? Narrations { get; set; }
}