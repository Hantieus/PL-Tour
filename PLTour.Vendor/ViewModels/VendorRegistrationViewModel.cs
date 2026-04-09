using System.ComponentModel.DataAnnotations;

namespace PLTour.Vendor.ViewModels
{
    public class VendorRegistrationViewModel
    {
        [Required(ErrorMessage = "Tên cửa hàng không được để trống")]
        [StringLength(200, ErrorMessage = "Tên cửa hàng không quá 200 ký tự")]
        [Display(Name = "Tên cửa hàng")]
        public string ShopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên chủ cửa hàng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên chủ cửa hàng không quá 100 ký tự")]
        [Display(Name = "Tên chủ cửa hàng")]
        public string OwnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Địa chỉ không quá 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Danh mục kinh doanh")]
        public int? CategoryId { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự")]
        [Display(Name = "Mô tả cửa hàng")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Logo cửa hàng")]
        public IFormFile? LogoFile { get; set; }

        [Display(Name = "Vị trí trên bản đồ (kinh độ)")]
        public double? Longitude { get; set; }

        [Display(Name = "Vị trí trên bản đồ (vĩ độ)")]
        public double? Latitude { get; set; }
    }
}