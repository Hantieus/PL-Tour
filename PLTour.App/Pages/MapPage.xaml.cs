#nullable disable
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Maui.ApplicationModel;
using PLTour.App.Models;
using PLTour.App.Services;
using System.Collections.ObjectModel;
using Plugin.Maui.Audio;

namespace PLTour.App.Pages;

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

    private IAudioPlayer _audioPlayer;
    private CancellationTokenSource _ttsCts;

    private readonly ApiService _apiService = new ApiService();
    private readonly LocationService _locationService;
    private readonly IAudioManager _audioManager;

    // THÊM: Dùng chung một HttpClient để tải file Audio mượt hơn, tránh lỗi ngắt stream
    private static readonly HttpClient _sharedHttpClient = new HttpClient();

    public MapPage(LocationService locationService, IAudioManager audioManager)
    {
        InitializeComponent();
        _locationService = locationService;
        _audioManager = audioManager;

        PoiListView.ItemsSource = SortedPois;

        InitializeMap();
        StartTracking();
        // Đã xóa gọi hàm LoadData ở đây để đưa xuống OnAppearing
    }

    // THÊM MỚI: Tự động tải lại bản đồ mỗi khi mở tab (để cập nhật đa ngôn ngữ)
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataFromApiAsync();
    }

    private async void BtnSpeak_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi == null)
        {
            System.Diagnostics.Debug.WriteLine("[LỖI NẶNG] Nút bấm không nhận được PoiModel. Hãy kiểm tra CommandParameter trong file XAML!");
            return;
        }

        // --- LOG KIỂM TRA ---
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Đang phát: {poi.Name}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] AudioUrl: {poi.AudioUrl}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] FullContent: {poi.FullContent}");

        if (poi.IsPlaying)
        {
            StopPlayback(poi);
            return;
        }

        poi.IsPlaying = true;

        try
        {
            // 1. ƯU TIÊN PHÁT FILE AUDIO (Nếu có Url)
            if (!string.IsNullOrEmpty(poi.AudioUrl))
            {
                // Dùng _sharedHttpClient thay vì tạo mới để tránh lỗi stream
                var stream = await _sharedHttpClient.GetStreamAsync(poi.AudioUrl);
                _audioPlayer = _audioManager.CreatePlayer(stream);

                _audioPlayer.PlaybackEnded += (s, args) =>
                    MainThread.BeginInvokeOnMainThread(() => poi.IsPlaying = false);

                _audioPlayer.Play();
            }
            // 2. NẾU KHÔNG CÓ AUDIO THÌ MỚI ĐỌC TTS
            else
            {
                _ttsCts = new CancellationTokenSource();
                // Ưu tiên FullContent, nếu rỗng mới lấy Description
                string content = string.IsNullOrWhiteSpace(poi.FullContent) ? poi.Description : poi.FullContent;

                if (string.IsNullOrWhiteSpace(content)) content = "Không có thông tin thuyết minh.";

                await TextToSpeech.SpeakAsync($"{poi.Name}. {content}", _ttsCts.Token);

                poi.IsPlaying = false;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi âm thanh: {ex.Message}");
            poi.IsPlaying = false;
        }
    }

    private void StopPlayback(PoiModel poi)
    {
        poi.IsPlaying = false;

        if (_audioPlayer != null)
        {
            if (_audioPlayer.IsPlaying) _audioPlayer.Stop();
            _audioPlayer.Dispose();
            _audioPlayer = null;
        }

        if (_ttsCts != null)
        {
            _ttsCts.Cancel();
            _ttsCts.Dispose();
            _ttsCts = null;
        }
    }

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

        // ĐÃ SỬA: Lọc Category bỏ qua viết hoa/viết thường và khoảng trắng thừa để không bị lỗi 0 kết quả
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

    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private async void Back_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//home");
    private void VoiceSearch_Clicked(object sender, EventArgs e) => txtSearch.Text = "Đang nghe...";

    // =================================================================
    // ĐÃ SỬA: Gọi hàm GetAllLocationsAsync() thay vì gọi Tour
    // =================================================================
    async Task LoadDataFromApiAsync()
    {
        try
        {
            // Đi tự do: Gọi trực tiếp lấy toàn bộ POI hiện có
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

    private void BtnViewMap_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null && mapView != null)
        {
            var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
            mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
            mapView.Map.Navigator.ZoomTo(1.5);
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
}