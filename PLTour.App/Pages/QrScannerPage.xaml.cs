using Microsoft.Maui.Controls;

namespace PLTour.App.Pages
{
    public partial class QrScannerPage : ContentPage
    {
        public QrScannerPage()
        {
            InitializeComponent();
        }

        private async void BtnDemoScan_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Chức năng Camera quét mã QR (như ZXing.Net.Maui) sẽ được tích hợp ở các giai đoạn sau của dự án.", "Đã hiểu");
        }
    }
}