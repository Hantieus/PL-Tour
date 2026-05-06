namespace PLTour.Admin.ViewModels
{
	public class ErrorViewModel
	{
		public string RequestId { get; set; } = string.Empty;
		public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

		// Thêm property này để hiển thị thông báo lỗi chi tiết
		public string ErrorMessage { get; set; } = string.Empty;
	}
}