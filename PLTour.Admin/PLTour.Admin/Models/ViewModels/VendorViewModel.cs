using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PLTour.Admin.Models.ViewModels
{
    public class VendorViewModel
    {
        public int VendorId { get; set; }

        [Required(ErrorMessage = "Tên cửa hàng không được để trống")]
        [StringLength(200, ErrorMessage = "Tên cửa hàng không quá 200 ký tự")]
        [Display(Name = "Tên cửa hàng")]
        public string ShopName { get; set; }

        [Required(ErrorMessage = "Tên chủ cửa hàng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên chủ cửa hàng không quá 100 ký tự")]
        [Display(Name = "Tên chủ cửa hàng")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không quá 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Danh mục")]
        public int? CategoryId { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Logo")]
        public IFormFile LogoFile { get; set; }

        [Display(Name = "Kinh độ")]
        public double? Longitude { get; set; }

        [Display(Name = "Vĩ độ")]
        public double? Latitude { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; }

        [Display(Name = "Ghi chú")]
        public string Notes { get; set; }
    }
}