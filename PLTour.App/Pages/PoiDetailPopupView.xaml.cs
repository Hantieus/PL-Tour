using PLTour.App.Models;
using System;

namespace PLTour.App.Pages
{
    public partial class PoiDetailPopupView : ContentView
    {
        // Sự kiện để báo cho trang cha biết cần đóng popup hoặc phát âm thanh
        public event EventHandler? CloseRequested;
        public event EventHandler<PoiModel>? SpeakRequested;

        public PoiDetailPopupView()
        {
            InitializeComponent();
        }

        // Khi nhấn nút X
        private void ClosePopup_Clicked(object sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // Khi nhấn nút Nghe/Dừng thuyết minh
        private void BtnSpeak_Clicked(object sender, EventArgs e)
        {
            if (BindingContext is PoiModel poi)
            {
                SpeakRequested?.Invoke(this, poi);
            }
        }
    }
}