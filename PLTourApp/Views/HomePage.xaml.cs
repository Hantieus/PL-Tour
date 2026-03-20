using PLTourApp.Database;
using PLTourApp.ViewModels;
using PLTourApp.Services;
using PLTourApp.Engines;
using System;

namespace PLTourApp.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Console.WriteLine("HomePage Loaded");
        }

        // ===== QUICK ACTION =====

        private async void OnMapClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapPage());
        }

        private async void OnPoiClicked(object sender, EventArgs e)
        {
            var db = new SQLiteHelper();
            var vm = new MapViewModel(db);

            await Navigation.PushAsync(new PoiListPage(vm));
        }

        private async void OnQRClicked(object sender, EventArgs e)
        {
            // 🔥 FIX LỖI constructor
            var db = new SQLiteHelper();
            var audio = new AudioService();
            var tts = new TTSService();
            var engine = new NarrationEngine(audio, tts, db);

            await Navigation.PushAsync(new QRScannerPage(db, engine));
        }

        private async void OnAnalyticsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AnalyticsPage());
        }

        // ===== BUTTON =====

        private async void OnCreateTourClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Thông báo", "Tạo tour mới", "OK");
        }

        // ===== ITEM =====

        private async void OnLocationTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Địa điểm", "Xem chi tiết địa điểm", "OK");
        }
    }
}