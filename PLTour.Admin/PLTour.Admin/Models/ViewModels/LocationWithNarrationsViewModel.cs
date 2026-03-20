using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace PLTour.Admin.Models.ViewModels
{
    public class LocationWithNarrationsViewModel
    {
        // Thông tin Location
        public int LocationId { get; set; }

        [Required(ErrorMessage = "Tên địa điểm không được để trống")]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vĩ độ không được để trống")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Kinh độ không được để trống")]
        public double Longitude { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Danh mục không được để trống")]
        public int CategoryId { get; set; }

        public string? ImageUrl { get; set; }
        public IFormFile? ImageFile { get; set; }

        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;

        // Danh sách các bài thuyết minh theo ngôn ngữ
        public List<NarrationViewModel> Narrations { get; set; } = new List<NarrationViewModel>();

        // Để hiển thị dropdown
        public SelectList? LanguageList { get; set; }
        public SelectList? CategoryList { get; set; }
    }

    public class NarrationViewModel
    {
        public int NarrationId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngôn ngữ")]
        public int LanguageId { get; set; }

        public string? LanguageName { get; set; }
        public string? LanguageCode { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(500)]
        public string Title { get; set; }

        [Required(ErrorMessage = "Nội dung thuyết minh không được để trống")]
        public string Content { get; set; }

        public string? AudioUrl { get; set; }
        public IFormFile? AudioFile { get; set; }

        public int Duration { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        // Để đánh dấu xóa audio cũ
        public bool RemoveAudio { get; set; }
    }
}