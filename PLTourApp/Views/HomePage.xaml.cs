using PLTourApp.Database;
using PLTourApp.ViewModels;
using PLTourApp.Services;
using PLTourApp.Engines;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;

namespace PLTourApp.Views
{
    public partial class HomePage : ContentPage
    {
        private HttpClient _httpClient = new HttpClient();

        public HomePage()
        {
            InitializeComponent();
        }

        // 🔥 LOAD DATA TỪ API
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // ⚠️ ĐỔI URL THEO API CỦA BẠN
                var url = "https://10.0.2.2:7054/api/tours";

                var tours = await _httpClient.GetFromJsonAsync<List<Tour>>(url);

                TourListView.ItemsSource = tours;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi API", ex.Message, "OK");
            }
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
            await DisplayAlert("Thông báo", "Tạo tour mới (sẽ làm ở bước sau)", "OK");
        }

        // ===== ITEM =====

        private async void OnLocationTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Địa điểm", "Xem chi tiết địa điểm", "OK");
        }
    }

    // 🔥 MODEL (thêm vào nếu chưa có)
    public class Tour
    {
        public int id { get; set; }
        public string name { get; set; }
        public string location { get; set; }
        public double price { get; set; }
        public string imageUrl { get; set; }
    }
}