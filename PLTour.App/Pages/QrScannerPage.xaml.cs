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
            await this.DisplayAlertAsync("Thông báo", "Chúc nang Camera quét mã QR (nhú ZXing.Net.Maui) sê duoc tich hop o các giai doan sau cúa du án.", "Dã hiêu");
        }
    }
}