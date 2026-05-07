using PLTour.App.Models;
using Microsoft.Maui.Controls;

namespace PLTour.App.Pages
{
    public partial class PoiDetailPopupView : ContentView
    {
        public event EventHandler? CloseRequested;
        public event EventHandler<PoiModel>? SpeakRequested;
        public event EventHandler<PoiModel>? ViewMapRequested;

        public event EventHandler? SpeakButtonClicked;
        public event EventHandler<PoiModel>? ViewMapButtonClicked;

        public PoiDetailPopupView()
        {
            InitializeComponent();
        }

        public void ShowPopup(PoiModel poi)
        {
            BindingContext = poi;
            IsVisible = true;
        }

        public void HidePopup()
        {
            IsVisible = false;
            BindingContext = null;
        }

        private void ClosePopup_Clicked(object sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnSpeak_Clicked(object sender, EventArgs e)
        {
            if (BindingContext is PoiModel poi)
            {
                SpeakRequested?.Invoke(this, poi);
                SpeakButtonClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private void BtnViewMap_Clicked(object sender, EventArgs e)
        {
            if (BindingContext is PoiModel poi)
            {
                ViewMapRequested?.Invoke(this, poi);
                ViewMapButtonClicked?.Invoke(this, poi);
            }
        }
    }
}