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
using PLTour.Share.Models;
using PLTour.Shared.Models.DTO; // Dùng để gọi DTO Tracking
using System.Collections.ObjectModel;
using System.Net.Http.Json;

namespace PLTour.App.Pages;

// Class dùng để hứng link audio trả về từ Backend
public class AudioResponse
{
    public string Url { get; set; }
}

public partial class MapPage : ContentPage
{
    MapView mapView;
    MemoryLayer _userLocationLayer;
    MemoryLayer _poiLayer;
    List<PoiModel> _allPois = new List<PoiModel>();
    public ObservableCollection<PoiModel> SortedPois { get; set; } = new();

    bool _isFirstLocation = true;
    bool isMapExpanded = false;
    string _currentCategory = "Tất cả";

    private CancellationTokenSource _ttsCts;
    private PoiModel _currentPlayingPoi;

    // BIẾN ĐỂ TÍNH THỜI GIAN NGHE (TRACKING)
    private DateTime? _playbackStartTime;
    private int _currentPoiIdTracked;

    private readonly ApiService _apiService = new ApiService();
    private readonly LocationService _locationService;
    private static readonly HttpClient _sharedHttpClient = new HttpClient();

    public MapPage(LocationService locationService)
    {
        InitializeComponent();
        _locationService = locationService;
        PoiListView.ItemsSource = SortedPois;

        InitializeMap();
        StartTracking();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataFromApiAsync();
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ GIAO DIỆN & BẢN ĐỒ
    // ==========================================
    private void InitializeMap()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MapContainer.Children.Clear();
            mapView = new MapView();
            mapView.Map = new Mapsui.Map();
            mapView.Map.Widgets.Clear();
            mapView.Map.Layers.Add(OpenStreetMap.CreateTileLayer());
            _userLocationLayer = new MemoryLayer { Name = "User" };
            mapView.Map.Layers.Add(_userLocationLayer);
            _poiLayer = new MemoryLayer { Name = "POIs" };
            mapView.Map.Layers.Add(_poiLayer);
            MapContainer.Children.Add(mapView);
        });
    }

    private async void StartTracking()
    {
        while (true)
        {
            try
            {
                var location = await _locationService.GetAndSaveCurrentLocationAsync();
                if (location != null)
                {
                    UpdateUserLocationOnMap(location);
                    UpdateDistancesAndSort();
                }
            }
            catch { }
            await Task.Delay(5000);
        }
    }

    async Task LoadDataFromApiAsync()
    {
        try
        {
            var pois = await _apiService.GetAllLocationsAsync();
            if (pois != null)
            {
                _allPois.Clear();
                _allPois.AddRange(pois);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DrawPoisOnMap();
                    UpdateDistancesAndSort();
                });
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Lỗi tải POIs: {ex.Message}"); }
    }

    void DrawPoisOnMap()
    {
        if (mapView == null || _allPois == null) return;
        var poiFeatures = new List<IFeature>();
        foreach (var poi in _allPois)
        {
            var proj = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
            var feature = new PointFeature(new MPoint(proj.x, proj.y));
            feature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(poi.PinColor), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2), SymbolScale = 0.5 });
            feature.Styles.Add(new LabelStyle { Text = poi.Name, Offset = new Offset(0, -20), ForeColor = Mapsui.Styles.Color.Black, BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White), Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2) });
            poiFeatures.Add(feature);
        }
        _poiLayer.Features = poiFeatures;
        _poiLayer.DataHasChanged();
        mapView.RefreshGraphics();
    }

    private void UpdateUserLocationOnMap(Microsoft.Maui.Devices.Sensors.Location location)
    {
        if (mapView == null) return;
        var proj = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
        var mapPoint = new MPoint(proj.x, proj.y);
        var userFeature = new PointFeature(mapPoint);
        userFeature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3), SymbolScale = 0.6 });
        _userLocationLayer.Features = new List<IFeature> { userFeature };
        _userLocationLayer.DataHasChanged();
        if (_isFirstLocation)
        {
            mapView.Map.Navigator.CenterOn(mapPoint);
            mapView.Map.Navigator.ZoomTo(2);
            _isFirstLocation = false;
        }
        MainThread.BeginInvokeOnMainThread(() => mapView?.RefreshGraphics());
    }

    private void UpdateDistancesAndSort()
    {
        var userLoc = _locationService.CurrentLocation;
        if (userLoc == null || _allPois == null) return;
        foreach (var poi in _allPois)
        {
            double dist = CalculateDistance(userLoc.Latitude, userLoc.Longitude, poi.Lat, poi.Lng);
            poi.DistanceMeters = dist;
            poi.Address = dist < 1000 ? $"{Math.Round(dist)} m" : $"{(dist / 1000.0):F1} km";
        }

        var filtered = _currentCategory == "Tất cả"
            ? _allPois.OrderBy(p => p.DistanceMeters).ToList()
            : _allPois.Where(p => p.Category?.Trim().ToLower() == _currentCategory.Trim().ToLower())
                      .OrderBy(p => p.DistanceMeters).ToList();

        MainThread.BeginInvokeOnMainThread(() => {
            if (SortedPois.Count == 0 || (filtered.Count > 0 && SortedPois[0].Name != filtered[0].Name))
            {
                SortedPois.Clear();
                foreach (var p in filtered) SortedPois.Add(p);
            }
            else
            {
                var temp = PoiListView.ItemsSource;
                PoiListView.ItemsSource = null;
                PoiListView.ItemsSource = temp;
            }
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

    // ==========================================
    // XỬ LÝ SỰ KIỆN CLICK CỦA NGƯỜI DÙNG
    // ==========================================
    private void Tab_Clicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        _currentCategory = btn.CommandParameter.ToString();
        TabTatCa.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3");
        TabThamQuan.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3");
        TabAnUong.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3");
        TabSuKien.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3");
        btn.TextColor = Microsoft.Maui.Graphics.Colors.White;
        btn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2A9D8F");
        UpdateDistancesAndSort();
    }

    private void ToggleMapSize_Clicked(object sender, EventArgs e)
    {
        isMapExpanded = !isMapExpanded;
        if (isMapExpanded) { Grid.SetRowSpan(MapSection, 2); InfoPanel.IsVisible = false; BtnToggleMap.Text = "Small 📂"; }
        else { Grid.SetRowSpan(MapSection, 1); InfoPanel.IsVisible = true; BtnToggleMap.Text = "Full 🔲"; }
    }

    private void CurrentLocation_Clicked(object sender, EventArgs e)
    {
        var loc = _locationService.CurrentLocation;
        if (loc != null && mapView != null)
        {
            var p = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude);
            mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
            mapView.Map.Navigator.ZoomTo(1.5);
        }
    }

    private async void Back_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//home");
    private void VoiceSearch_Clicked(object sender, EventArgs e) => txtSearch.Text = "Đang nghe...";

    private void BtnViewMap_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null && mapView != null)
        {
            var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
            mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
            mapView.Map.Navigator.ZoomTo(1.5);

            // TRACKING: Xem bản đồ
            _ = AnalyticsService.Instance.TrackEventAsync("view_location", new AnalyticsEventDto { LocationId = poi.Id });
        }
    }

    private async void BtnRoute_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null)
            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(
                new Microsoft.Maui.Devices.Sensors.Location(poi.Lat, poi.Lng),
                new MapLaunchOptions { Name = poi.Name, NavigationMode = NavigationMode.Driving });
    }

    private void PoiItem_Tapped(object sender, TappedEventArgs e)
    {
        var poi = e.Parameter as PoiModel ?? (sender as Border)?.BindingContext as PoiModel;
        if (poi != null)
        {
            PoiDetailPopup.BindingContext = poi;
            PoiDetailPopup.IsVisible = true;

            // TRACKING: Xem chi tiết
            _ = AnalyticsService.Instance.TrackEventAsync("view_location", new AnalyticsEventDto { LocationId = poi.Id });
        }
    }

    private void ClosePopup_Clicked(object sender, EventArgs e)
    {
        PoiDetailPopup.IsVisible = false;
        PoiDetailPopup.BindingContext = null;
    }

    // ==========================================
    // XỬ LÝ AUDIO, GEOFENCING VÀ THỜI GIAN NGHE
    // ==========================================
    private async void BtnSpeak_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi == null) return;

        // Bấm để Dừng (Kích hoạt tracking Duration)
        if (poi.IsPlaying)
        {
            StopPlayback(poi);
            return;
        }

        if (_currentPlayingPoi != null) StopPlayback(_currentPlayingPoi);

        // Geofencing
        var userLoc = _locationService.CurrentLocation;
        string eventToTrack = "listen_remote";

        if (userLoc != null)
        {
            // poi.DistanceMeters đã được tính liên tục ở hàm UpdateDistancesAndSort
            if (poi.DistanceMeters <= poi.Radius)
            {
                eventToTrack = "listen_onsite";
            }
            else
            {
                bool confirm = await DisplayAlert("Bạn đang ở xa",
                    $"Bạn cách {poi.Name} khoảng {Math.Round(poi.DistanceMeters)}m. Bạn có muốn nghe thuyết minh ảo từ xa không?",
                    "Nghe", "Hủy bỏ");
                if (!confirm) return;
            }
        }

        poi.IsPlaying = true;
        _currentPlayingPoi = poi;
        _playbackStartTime = DateTime.UtcNow;
        _currentPoiIdTracked = poi.Id;

        // Gửi Tracking: Báo server là bắt đầu nghe
        _ = AnalyticsService.Instance.TrackEventAsync(eventToTrack, new AnalyticsEventDto
        {
            LocationId = poi.Id,
            LanguageCode = poi.LanguageCode ?? "vi",
            HasAudio = true
        });

        try
        {
            if (!string.IsNullOrEmpty(poi.AudioUrl))
            {
                string finalUrl = FixLocalhostUrl(poi.AudioUrl);
                AudioPlayer.Source = MediaSource.FromUri(finalUrl);
                AudioPlayer.Play();
            }
            else if (!string.IsNullOrWhiteSpace(poi.FullContent))
            {
                string baseUrl = "https://q0x087zj-7291.asse.devtunnels.ms";
                string generateApiUrl = $"{baseUrl}/api/audio/generate?text={Uri.EscapeDataString(poi.FullContent)}&langCode={poi.LanguageCode}&narrationId={poi.NarrationId}";

                var response = await _sharedHttpClient.GetFromJsonAsync<AudioResponse>(generateApiUrl);

                if (response != null && !string.IsNullOrEmpty(response.Url))
                {
                    string finalAudioUrl = FixLocalhostUrl(response.Url);
                    AudioPlayer.Source = MediaSource.FromUri(finalAudioUrl);
                    AudioPlayer.Play();
                }
                else await ReadTextOffline(poi);
            }
            else await ReadTextOffline(poi);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi gọi API âm thanh: {ex.Message}");
            await ReadTextOffline(poi);
        }
    }

    private void StopPlayback(PoiModel poi)
    {
        poi.IsPlaying = false;
        AudioPlayer.Stop();

        if (_ttsCts != null)
        {
            _ttsCts.Cancel();
            _ttsCts.Dispose();
            _ttsCts = null;
        }

        if (_currentPlayingPoi == poi) _currentPlayingPoi = null;

        SendListenDurationTracking();
    }

    private void AudioPlayer_MediaEnded(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_currentPlayingPoi != null)
            {
                _currentPlayingPoi.IsPlaying = false;
                _currentPlayingPoi = null;
                SendListenDurationTracking();
            }
        });
    }

    private void SendListenDurationTracking()
    {
        if (_playbackStartTime.HasValue)
        {
            int secondsListened = (int)(DateTime.UtcNow - _playbackStartTime.Value).TotalSeconds;
            _ = AnalyticsService.Instance.TrackEventAsync("listen_duration", new AnalyticsEventDto
            {
                LocationId = _currentPoiIdTracked,
                Duration = secondsListened
            });
            _playbackStartTime = null;
        }
    }

    private string FixLocalhostUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.Contains("localhost"))
        {
            url = url.Replace("localhost:7291", "q0x087zj-7291.asse.devtunnels.ms");
            url = url.Replace("http://", "https://");
        }
        return url;
    }

    private async Task ReadTextOffline(PoiModel poi)
    {
        _ttsCts = new CancellationTokenSource();
        string content = string.IsNullOrWhiteSpace(poi.FullContent) ? poi.Description : poi.FullContent;
        if (string.IsNullOrWhiteSpace(content)) content = "Không có thông tin thuyết minh.";

        var locales = await TextToSpeech.Default.GetLocalesAsync();
        var targetLocale = locales.FirstOrDefault(l => l.Language.StartsWith(poi.LanguageCode ?? "vi", StringComparison.OrdinalIgnoreCase));

        var speechOptions = new SpeechOptions { Locale = targetLocale };

        await TextToSpeech.SpeakAsync($"{poi.Name}. {content}", speechOptions, _ttsCts.Token);

        poi.IsPlaying = false;
        if (_currentPlayingPoi == poi) _currentPlayingPoi = null;

        SendListenDurationTracking();
    }
}