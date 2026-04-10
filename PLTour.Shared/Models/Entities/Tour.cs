using System.ComponentModel.DataAnnotations;

namespace PLTour.Shared.Models.Entities;

public class Tour
{
    [Key]
    public int TourId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(100)]
    public string Duration { get; set; } // VD: "3 tiếng 30 phút"

    public string IntroText { get; set; }

    [StringLength(500)]
    public string ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation property: Mối quan hệ n-n với Location thông qua bảng trung gian
    public virtual ICollection<TourLocation> TourLocations { get; set; }
}