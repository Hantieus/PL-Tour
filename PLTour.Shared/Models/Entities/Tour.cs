using System.ComponentModel.DataAnnotations;

namespace PLTour.Shared.Models.Entities
{
    public class Tour
    {
        [Key]
        public int TourId { get; set; }

        [Required(ErrorMessage = "Tên tour không được để trống")]
        [StringLength(200, ErrorMessage = "Tên tour không quá 200 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Range(0, 1000, ErrorMessage = "Thời lượng từ 0 đến 1000 phút")]
        public int Duration { get; set; } // phút

        [StringLength(1000, ErrorMessage = "Giới thiệu không quá 1000 ký tự")]
        public string? IntroText { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        // Navigation
        public virtual ICollection<TourLocation> TourLocations { get; set; } = new List<TourLocation>();
    }
}