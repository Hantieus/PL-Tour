#nullable disable
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using PLTour.App.Models;
using PLTour.App.Services;
using PLTour.Shared.Models.DTO;
using System.Collections.ObjectModel;

// Định nghĩa bí danh để tránh xung đột
using MauiColor = Microsoft.Maui.Graphics.Color;
using MapsuiColor = Mapsui.Styles.Color;

namespace PLTour.App.Pages
{
    public partial class MapPage : ContentPage
    {
        // 1. KHAI BÁO BIẾN
        MapView mapView;
        MemoryLayer _userLocationLayer;
        MemoryLayer _poiLayer;
        List<PoiModel> _allPois = new List<PoiModel>();
        public ObservableCollection<PoiModel> SortedPois { get; set; } = new();

        string _currentCategory = "Tất cả";
        bool _isFirstLocation = true;
        bool isMapExpanded = false;

        private CancellationTokenSource _ttsCts;
        private PoiModel _currentPlayingPoi;
        private HashSet<int> _autoPlayedPoiIds = new HashSet<int>();

        private readonly ApiService _apiService;
        private readonly LocationService _locationService;
        private readonly RouteTrackingService _routeTrackingService;

        public MapPage(LocationService locationService, RouteTrackingService routeTrackingService, ApiService apiService)
        {
            InitializeComponent();
            _locationService = locationService;
            _routeTrackingService = routeTrackingService;
            _apiService = apiService;

            PoiListView.ItemsSource = SortedPois;

            InitializeMap();
            LoadCategoryTabs();
            StartTracking();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataFromApiAsync();
        }

        // 2. LOGIC DANH MỤC (PHÂN TRANG THEO LOẠI)
        private void LoadCategoryTabs()
        {
            var categories = new List<string> { "Tất cả", PoiCategories.ThamQuan, PoiCategories.AnUong, PoiCategories.SuKien };
            CategoryTabsContainer.Children.Clear();

            foreach (var cat in categories)
            {
                var btn = new Button
                {
                    Text = cat,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 15,
                    Margin = new Thickness(0, 0, 5, 0),
                    HeightRequest = 38,
                    // SỬA LỖI: Dùng MauiColor thay vì Color
                    BackgroundColor = (cat == _currentCategory) ? MauiColor.FromArgb("#2A9D8F") : MauiColor.FromArgb("#D3D3D3"),
                    TextColor = (cat == _currentCategory) ? Colors.White : Colors.DimGray,
                    CommandParameter = cat
                };
                btn.Clicked += Tab_Clicked;
                CategoryTabsContainer.Children.Add(btn);
            }
        }

        private void Tab_Clicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            _currentCategory = btn.CommandParameter.ToString();

            foreach (Button b in CategoryTabsContainer.Children)
            {
                bool isSelected = b.CommandParameter.ToString() == _currentCategory;
                // SỬA LỖI: Dùng MauiColor thay vì Color
                b.BackgroundColor = isSelected ? MauiColor.FromArgb("#2A9D8F") : MauiColor.FromArgb("#D3D3D3");
                b.TextColor = isSelected ? Colors.White : Colors.DimGray;
            }

            UpdateDistancesAndSort();
        }

        // 3. LOGIC TRACKING & GEOFENCING
        private async void StartTracking()
        {
            var initialLoc = await _locationService.GetAndSaveCurrentLocationAsync();
            if (initialLoc != null) UpdateUserLocationOnMap(initialLoc);

            while (true)
            {
                try
                {
                    var location = await _locationService.GetAdaptiveLocationAsync();
                    if (location != null)
                    {
                        UpdateUserLocationOnMap(location);
                        UpdateDistancesAndSort();
                        await _routeTrackingService.ProcessLocationForRoute(location, _allPois);

                        if (_locationService.ShouldSendHeartbeat())
                            _ = AnalyticsService.Instance.TrackLocationPingAsync(location.Latitude, location.Longitude);

                        CheckGeofenceAutoPlay();
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                await Task.Delay(_locationService.GetAdaptiveInterval());
            }
        }

        private void CheckGeofenceAutoPlay()
        {
            if (_currentPlayingPoi != null && _currentPlayingPoi.IsPlaying) return;
            var closestPoi = _allPois.Where(p => p.DistanceMeters <= p.Radius && !_autoPlayedPoiIds.Contains(p.Id))
                                     .OrderBy(p => p.DistanceMeters).FirstOrDefault();
            if (closestPoi != null)
            {
                _autoPlayedPoiIds.Add(closestPoi.Id);
                PlayAudioForPoi(closestPoi, true);
            }
        }

        // 4. XỬ LÝ SỰ KIỆN TỪ COMPONENT DÙNG CHUNG
        private void PoiDetailPopup_CloseRequested(object sender, EventArgs e)
        {
            PoiDetailPopup.IsVisible = false;
        }

        private void PoiDetailPopup_SpeakRequested(object sender, PoiModel poi)
        {
            if (poi != null)
            {
                bool isOnSite = poi.DistanceMeters <= poi.Radius;
                PlayAudioForPoi(poi, isOnSite);
            }
        }

        // 5. CÁC HÀM SỰ KIỆN GIAO DIỆN
        private void AudioPlayer_MediaEnded(object sender, EventArgs e)
        {
            if (_currentPlayingPoi != null) StopPlayback(_currentPlayingPoi);
        }

        private async void Back_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//home");

        private void VoiceSearch_Clicked(object sender, EventArgs e) => txtSearch.Text = "Đang nghe...";

        private void ToggleMapSize_Clicked(object sender, EventArgs e)
        {
            isMapExpanded = !isMapExpanded;
            Grid.SetRowSpan(MapSection, isMapExpanded ? 2 : 1);
            InfoPanel.IsVisible = !isMapExpanded;
            BtnToggleMap.Text = isMapExpanded ? "Small 📂" : "Full 🔲";
        }

        private void CurrentLocation_Clicked(object sender, EventArgs e)
        {
            var loc = _locationService.CurrentLocation;
            if (loc != null && mapView?.Map != null)
            {
                var p = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude);
                mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
                mapView.Map.Navigator.ZoomTo(1.5);
            }
        }

        private async void PoiItem_Tapped(object sender, EventArgs e)
        {
            var border = sender as Border;
            var poi = border?.BindingContext as PoiModel;
            if (poi != null)
            {
                PoiDetailPopup.BindingContext = poi;
                PoiDetailPopup.IsVisible = true;
                _ = AnalyticsService.Instance.TrackPoiViewAsync(poi.Id);

                if (poi.IsDiningCategory && poi.MenuItems.Count == 0 && poi.VendorId.HasValue)
                {
                    poi.IsLoadingMenu = true;
                    try
                    {
                        var products = await _apiService.GetProductsByVendorAsync(poi.VendorId.Value);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            poi.MenuItems = products;
                        });
                    }
                    finally
                    {
                        poi.IsLoadingMenu = false;
                    }
                }
            }
        }

        private void BtnViewMap_Clicked(object sender, EventArgs e)
        {
            var poi = (sender as Button)?.CommandParameter as PoiModel;
            if (poi != null && mapView?.Map != null)
            {
                var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
                mapView.Map.Navigator.ZoomTo(1.5);
            }
        }

        private async void BtnSpeak_Clicked(object sender, EventArgs e)
        {
            var poi = (sender as Button)?.CommandParameter as PoiModel;
            if (poi == null) return;
            if (poi.IsPlaying) { StopPlayback(poi); return; }

            bool isOnSite = poi.DistanceMeters <= poi.Radius;
            if (!isOnSite)
            {
                bool confirm = await DisplayAlert("Bạn đang ở xa", $"Bạn cách {poi.Name} {poi.DistanceText}. Nghe từ xa?", "Nghe", "Hủy");
                if (!confirm) return;
            }
            PlayAudioForPoi(poi, isOnSite);
        }

        private async void BtnRoute_Clicked(object sender, EventArgs e)
        {
            var poi = (sender as Button)?.CommandParameter as PoiModel;
            if (poi != null) await Microsoft.Maui.ApplicationModel.Map.OpenAsync(poi.Lat, poi.Lng, new MapLaunchOptions { Name = poi.Name });
        }

        // 6. HÀM HỖ TRỢ PHÁT AUDIO & MAP
        private async void PlayAudioForPoi(PoiModel poi, bool isOnSite)
        {
            if (_currentPlayingPoi != null) StopPlayback(_currentPlayingPoi);
            poi.IsPlaying = true;
            _currentPlayingPoi = poi;
            _ = AnalyticsService.Instance.TrackAudioStartAsync(poi.Id, poi.LanguageCode ?? "vi", isOnSite);

            try
            {
                if (!string.IsNullOrEmpty(poi.AudioUrl))
                {
                    AudioPlayer.Source = MediaSource.FromUri(poi.AudioUrl);
                    AudioPlayer.Play();
                }
                else { await ReadTextOffline(poi); }
            }
            catch { await ReadTextOffline(poi); }
        }

        private void StopPlayback(PoiModel poi)
        {
            if (poi != null) poi.IsPlaying = false;
            AudioPlayer.Stop();
            if (_ttsCts != null) { _ttsCts.Cancel(); _ttsCts.Dispose(); _ttsCts = null; }
            _currentPlayingPoi = null;
            _ = AnalyticsService.Instance.TrackAudioStopAsync();
        }

        private async Task ReadTextOffline(PoiModel poi)
        {
            _ttsCts = new CancellationTokenSource();
            await TextToSpeech.SpeakAsync($"{poi.Name}. {poi.FullContent}", _ttsCts.Token);
            StopPlayback(poi);
        }

        private void InitializeMap()
        {
            MainThread.BeginInvokeOnMainThread(() => {
                MapContainer.Children.Clear();
                mapView = new MapView();
                mapView.Map = new Mapsui.Map();
                mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
                _userLocationLayer = new MemoryLayer { Name = "User" };
                mapView.Map.Layers.Add(_userLocationLayer);
                _poiLayer = new MemoryLayer { Name = "POIs" };
                mapView.Map.Layers.Add(_poiLayer);
                MapContainer.Children.Add(mapView);
            });
        }

        private async Task LoadDataFromApiAsync()
        {
            try
            {
                var pois = await _apiService.GetAllLocationsAsync();
                if (pois != null) { _allPois.Clear(); _allPois.AddRange(pois); UpdateDistancesAndSort(); DrawPoisOnMap(); }
            }
            catch { }
        }

        private void UpdateUserLocationOnMap(Microsoft.Maui.Devices.Sensors.Location location)
        {
            if (mapView?.Map == null) return;
            var proj = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            var mapPoint = new MPoint(proj.x, proj.y);
            // SỬA LỖI: Dùng MapsuiColor cho bản đồ
            _userLocationLayer.Features = new List<IFeature> { new PointFeature(mapPoint) { Styles = { new SymbolStyle { Fill = new Mapsui.Styles.Brush(MapsuiColor.Blue), SymbolScale = 0.6 } } } };
            _userLocationLayer.DataHasChanged();
            if (_isFirstLocation) { mapView.Map.Navigator.CenterOn(mapPoint); mapView.Map.Navigator.ZoomTo(2); _isFirstLocation = false; }
            MainThread.BeginInvokeOnMainThread(() => mapView?.RefreshGraphics());
        }

        private void UpdateDistancesAndSort()
        {
            var userLoc = _locationService.CurrentLocation;
            if (_allPois == null || userLoc == null) return;

            foreach (var poi in _allPois)
            {
                double dist = Microsoft.Maui.Devices.Sensors.Location.CalculateDistance(userLoc, new Microsoft.Maui.Devices.Sensors.Location(poi.Lat, poi.Lng), DistanceUnits.Kilometers);
                poi.DistanceMeters = dist * 1000;
                poi.Address = poi.DistanceMeters < 1000 ? $"{(int)poi.DistanceMeters} m" : $"{dist:F1} km";
            }

            var filtered = _allPois
                .Where(p => _currentCategory == "Tất cả" || p.Category == _currentCategory)
                .OrderBy(p => p.DistanceMeters)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() => {
                SortedPois.Clear();
                foreach (var p in filtered) SortedPois.Add(p);
            });
        }

        private void DrawPoisOnMap()
        {
            if (mapView?.Map == null || _allPois == null) return;
            MainThread.BeginInvokeOnMainThread(() => {
                _poiLayer.Features = _allPois.Select(poi => {
                    var proj = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                    return new PointFeature(new MPoint(proj.x, proj.y)) { Styles = { new SymbolStyle { Fill = new Mapsui.Styles.Brush(poi.PinColor), SymbolScale = 0.5 } } };
                }).Cast<IFeature>().ToList();
                _poiLayer.DataHasChanged();
                mapView.RefreshGraphics();
            });
        }
    }
}