using System.Diagnostics;
using PLTour.App.Models;
using PLTour.App.Services;
using PLTour.Shared.Models.DTO;
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

        private readonly ApiService _apiService = new();
        private bool _isLoadingMenu;

        public PoiDetailPopupView()
        {
            InitializeComponent();
        }

        public async void ShowPopup(PoiModel poi)
        {
            BindingContext = poi;
            IsVisible = true;

            if (_isLoadingMenu)
                return;

            _isLoadingMenu = true;
            try
            {
                var products = await _apiService.GetProductsForPoiAsync(poi);
                Debug.WriteLine($"[MENU_LOG] URL={_apiService.BaseUrlForDebug}, POI={poi.Id}, Products={products.Count}");
                poi.SetStoreProducts(products);
                poi.RaiseLocalizedChanged();
            }
            finally
            {
                _isLoadingMenu = false;
            }
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