#nullable disable
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using PLTour.App.Models;
using PLTour.App.Services;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using Plugin.Maui.Audio;
using PLTour.Shared.Models.DTO;

namespace PLTour.App.Pages;

[QueryProperty(nameof(Tour), "SelectedTour")]
public partial class TourDetailPage : ContentPage
{
    private TourModel _tour;
    public TourModel Tour
    {
        get => _tour;
        set
        {
            _tour = value;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadTourData();
            });
        }
    }

    public ObservableCollection<PoiModel> SortedPois { get; set; } = new();

    private MapView mapView;
    private bool isMapExpanded = false;
    private bool _isFirstLocation = true;
    private MemoryLayer _userLocationLayer;
    private HashSet<int> _autoPlayedPoiIds = new HashSet<int>();
    private PoiModel _currentPlayingPoi;

    // QUẢN LÝ ÂM THANH
    private IAudioPlayer _audioPlayer;
    private CancellationTokenSource _ttsCts;

    private readonly LocationService _locationService;
    private readonly RouteTrackingService _routeTrackingService;
    private readonly ApiService _apiService; // Thêm ApiService để gọi Menu

    public TourDetailPage(LocationService locationService, RouteTrackingService routeTrackingService, ApiService apiService)
    {
        InitializeComponent();
        _locationService = locationService;
        _routeTrackingService = routeTrackingService;
        _apiService = apiService;

        PoiList.ItemsSource = SortedPois;
        StartTracking();
    }

    // ==========================================
    // 1. ADAPTIVE GPS & ROUTE TRACKING
    // ==========================================
    private async void StartTracking()
    {
        var initialLoc = await _locationService.GetAndSaveCurrentLocationAsync();
        if (initialLoc != null)
        {
            UpdateUserLocationOnMap(initialLoc);
            _ = AnalyticsService.Instance.TrackLocationPingAsync(initialLoc.Latitude, initialLoc.Longitude);
        }

        while (true)
        {
            try
            {
                var location = await _locationService.GetAdaptiveLocationAsync();
                if (location != null)
                {
                    UpdateUserLocationOnMap(location);
                    UpdateDistances();

                    if (_tour?.Pois != null)
                    {
                        await _routeTrackingService.ProcessLocationForRoute(location, _tour.Pois);
                    }

                    if (_locationService.ShouldSendHeartbeat())
                    {
                        _ = AnalyticsService.Instance.TrackLocationPingAsync(location.Latitude, location.Longitude);
                    }

                    CheckGeofenceAndAutoPlay(location);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Detail Tracking Error: {ex.Message}"); }

            await Task.Delay(_locationService.GetAdaptiveInterval());
        }
    }

    private void CheckGeofenceAndAutoPlay(Microsoft.Maui.Devices.Sensors.Location userLoc)
    {
        if (_currentPlayingPoi != null && _currentPlayingPoi.IsPlaying) return;
        if (_tour?.Pois == null) return;

        var closestPoi = _tour.Pois
            .Where(p => p.DistanceMeters <= p.Radius && !_autoPlayedPoiIds.Contains(p.Id))
            .OrderBy(p => p.DistanceMeters)
            .FirstOrDefault();

        if (closestPoi != null)
        {
            _autoPlayedPoiIds.Add(closestPoi.Id);
            PlayAudioForPoi(closestPoi, true);
        }
    }

    // ==========================================
    // 2. LOGIC CHI TIẾT POI & MENU
    // ==========================================

    // Hàm xử lý khi người dùng chạm vào một Border địa điểm trong danh sách
    private async void PoiItem_Tapped(object sender, EventArgs e)
    {
        var border = sender as Border;
        var poi = border?.BindingContext as PoiModel;

        if (poi != null)
        {
            // Hiển thị Popup
            PoiDetailPopup.BindingContext = poi;
            PoiDetailPopup.IsVisible = true;
            _ = AnalyticsService.Instance.TrackPoiViewAsync(poi.Id);

            // Kiểm tra và tải Menu nếu là địa điểm ăn uống
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
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Menu Load Error: {ex.Message}"); }
                finally
                {
                    poi.IsLoadingMenu = false;
                }
            }
        }
    }

    // Sự kiện đóng Popup từ Component dùng chung
    private void PoiDetailPopup_CloseRequested(object sender, EventArgs e)
    {
        PoiDetailPopup.IsVisible = false;
    }

    // Sự kiện nút Nghe từ bên trong Popup
    private void PoiDetailPopup_SpeakRequested(object sender, PoiModel poi)
    {
        if (poi != null)
        {
            bool isOnSite = poi.DistanceMeters <= poi.Radius;
            PlayAudioForPoi(poi, isOnSite);
        }
    }

    // ==========================================
    // 3. LOGIC PHÁT ÂM THANH
    // ==========================================
    private void BtnSpeakPoi_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi == null) return;

        if (poi.IsPlaying) { StopPlayback(poi); return; }
        if (_currentPlayingPoi != null) StopPlayback(_currentPlayingPoi);

        bool isOnSite = poi.DistanceMeters <= poi.Radius;
        PlayAudioForPoi(poi, isOnSite);
    }

    private async void PlayAudioForPoi(PoiModel poi, bool isOnSite)
    {
        poi.IsPlaying = true;
        _currentPlayingPoi = poi;

        await _routeTrackingService.RecordVisitAsync(poi, _locationService.CurrentLocation);
        _ = AnalyticsService.Instance.TrackAudioStartAsync(poi.Id, poi.LanguageCode ?? "vi", isOnSite);

        try
        {
            if (!string.IsNullOrEmpty(poi.AudioUrl))
            {
                using (var httpClient = new HttpClient())
                {
                    var stream = await httpClient.GetStreamAsync(poi.AudioUrl);
                    _audioPlayer = AudioManager.Current.CreatePlayer(stream);
                    _audioPlayer.PlaybackEnded += (s, args) => {
                        MainThread.BeginInvokeOnMainThread(() => poi.IsPlaying = false);
                        _currentPlayingPoi = null;
                        _ = AnalyticsService.Instance.TrackAudioStopAsync();
                    };
                    _audioPlayer.Play();
                }
            }
            else
            {
                _ttsCts = new CancellationTokenSource();
                string textToRead = string.IsNullOrEmpty(poi.FullContent) ? poi.Description : poi.FullContent;
                await TextToSpeech.SpeakAsync($"{poi.Name}. {textToRead}", _ttsCts.Token);
                poi.IsPlaying = false;
                _currentPlayingPoi = null;
                _ = AnalyticsService.Instance.TrackAudioStopAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi âm thanh: {ex.Message}");
            poi.IsPlaying = false;
            _currentPlayingPoi = null;
            _ = AnalyticsService.Instance.TrackAudioStopAsync();
        }
    }

    private void StopPlayback(PoiModel poi)
    {
        if (poi != null) poi.IsPlaying = false;
        if (_audioPlayer != null) { if (_audioPlayer.IsPlaying) _audioPlayer.Stop(); _audioPlayer.Dispose(); _audioPlayer = null; }
        if (_ttsCts != null) { _ttsCts.Cancel(); _ttsCts.Dispose(); _ttsCts = null; }
        if (_currentPlayingPoi == poi) _currentPlayingPoi = null;
        _ = AnalyticsService.Instance.TrackAudioStopAsync();
    }

    // ==========================================
    // 4. CÁC HÀM TIỆN ÍCH & BẢN ĐỒ
    // ==========================================
    private void LoadTourData() { if (_tour == null) return; lblTourName.Text = _tour.Name; InitializeMap(); UpdateDistances(); }

    private void UpdateUserLocationOnMap(Microsoft.Maui.Devices.Sensors.Location location)
    {
        if (mapView == null || _userLocationLayer == null) return;
        var proj = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
        var mapPoint = new MPoint(proj.x, proj.y);
        var userFeature = new PointFeature(mapPoint);
        userFeature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3), SymbolScale = 0.6 });
        _userLocationLayer.Features = new List<IFeature> { userFeature };
        _userLocationLayer.DataHasChanged();
        if (_isFirstLocation) { mapView.Map.Navigator.CenterOn(mapPoint); _isFirstLocation = false; }
        MainThread.BeginInvokeOnMainThread(() => mapView?.RefreshGraphics());
    }

    private void UpdateDistances()
    {
        var userLoc = _locationService.CurrentLocation;
        if (userLoc == null || _tour?.Pois == null) return;
        foreach (var poi in _tour.Pois)
        {
            double distMeters = CalculateDistance(userLoc.Latitude, userLoc.Longitude, poi.Lat, poi.Lng);
            poi.DistanceMeters = distMeters;
            poi.Address = distMeters < 1000 ? $"{Math.Round(distMeters)} m" : $"{(distMeters / 1000.0):F1} km";
        }
        var listSorted = _tour.Pois.OrderBy(p => p.DistanceMeters).ToList();
        MainThread.BeginInvokeOnMainThread(() => {
            if (SortedPois.Count == 0 || SortedPois[0].Name != listSorted[0].Name) { SortedPois.Clear(); foreach (var p in listSorted) SortedPois.Add(p); }
            else { var temp = PoiList.ItemsSource; PoiList.ItemsSource = null; PoiList.ItemsSource = temp; }
        });
    }

    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private void InitializeMap()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                MapContainer.Children.Clear();
                mapView = new MapView();
                mapView.Map = new Mapsui.Map();
                mapView.Map.Widgets.Clear();
                mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
                _userLocationLayer = new MemoryLayer { Name = "UserLocation" };
                mapView.Map.Layers.Add(_userLocationLayer);
                if (_tour?.Pois != null && _tour.Pois.Any())
                {
                    var poiLayer = new MemoryLayer { Name = "TourPois" };
                    var features = new List<IFeature>();
                    foreach (var poi in _tour.Pois)
                    {
                        var proj = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
                        var feat = new PointFeature(new MPoint(proj.x, proj.y));
                        feat.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Red), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2), SymbolScale = 0.6 });
                        feat.Styles.Add(new LabelStyle { Text = poi.Name, Offset = new Offset(0, -20), ForeColor = Mapsui.Styles.Color.Black, BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White), Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2) });
                        features.Add(feat);
                    }
                    poiLayer.Features = features;
                    mapView.Map.Layers.Add(poiLayer);
                    var first = _tour.Pois.First();
                    var centerProj = SphericalMercator.FromLonLat(first.Lng, first.Lat);
                    mapView.Map.Navigator.CenterOn(new MPoint(centerProj.x, centerProj.y));
                    mapView.Map.Navigator.ZoomTo(2);
                }
                MapContainer.Children.Add(mapView);
                var currentLoc = _locationService.CurrentLocation;
                if (currentLoc != null) UpdateUserLocationOnMap(currentLoc);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        });
    }

    private void CurrentLocation_Clicked(object sender, EventArgs e) { var loc = _locationService.CurrentLocation; if (loc != null && mapView != null) { var p = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude); mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y)); mapView.Map.Navigator.ZoomTo(1.2); } }
    private void ToggleMapSize_Clicked(object sender, EventArgs e) { isMapExpanded = !isMapExpanded; if (isMapExpanded) { Grid.SetRowSpan(MapSection, 2); InfoPanel.IsVisible = false; BtnToggleMap.Text = "Small 📂"; } else { Grid.SetRowSpan(MapSection, 1); InfoPanel.IsVisible = true; BtnToggleMap.Text = "Full 🔲"; } }
    private async void Back_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private void BtnViewMap_Clicked(object sender, EventArgs e) { var poi = (sender as Button)?.CommandParameter as PoiModel; if (poi != null && mapView != null) { var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat); mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y)); mapView.Map.Navigator.ZoomTo(1.5); _ = AnalyticsService.Instance.TrackPoiViewAsync(poi.Id); } }
    private async void SpeakIntro_Clicked(object sender, EventArgs e) { if (_tour != null) await TextToSpeech.SpeakAsync(_tour.IntroText); }
    private async void BtnRoute_Clicked(object sender, EventArgs e) { var poi = (sender as Button)?.CommandParameter as PoiModel; if (poi != null) await Microsoft.Maui.ApplicationModel.Map.OpenAsync(new Microsoft.Maui.Devices.Sensors.Location(poi.Lat, poi.Lng), new MapLaunchOptions { Name = poi.Name, NavigationMode = NavigationMode.Driving }); }
}