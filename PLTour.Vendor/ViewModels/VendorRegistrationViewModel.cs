using System.ComponentModel.DataAnnotations;

namespace PLTour.Vendor.ViewModels
{
    public class VendorRegistrationViewModel
    {
        [Required(ErrorMessage = "Tên cửa hàng không được để trống")]
        [StringLength(200, ErrorMessage = "Tên cửa hàng không quá 200 ký tự")]
        [Display(Name = "Tên cửa hàng")]
        public string BusinessName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên người liên hệ không được để trống")]
        [StringLength(100, ErrorMessage = "Tên người liên hệ không quá 100 ký tự")]
        [Display(Name = "Tên người liên hệ")]
        public string ContactName { get; set; } = string.Empty;

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

        [Display(Name = "Danh mục kinh doanh")]
        public int? CategoryId { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả không quá 1000 ký tự")]
        [Display(Name = "Mô tả cửa hàng")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Avatar / logo")]
        public IFormFile? AvatarFile { get; set; }

        [Display(Name = "Gói đăng ký")]
        public string Plan { get; set; } = "Free";
    }
}