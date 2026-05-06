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
using System.ComponentModel;
using System.Net.Http.Json;
using System.Threading;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;

namespace PLTour.App.Pages;

[QueryProperty(nameof(TourId), "TourId")]
[QueryProperty(nameof(ScannedPoiName), "ScannedPoiName")]
public partial class MapPage : ContentPage
{
    MapView mapView;
    MemoryLayer _userLocationLayer;
    MemoryLayer _poiLayer;
    List<PoiModel> _allPois = new List<PoiModel>();
    private string _tourId;
    private string _scannedPoiName = string.Empty;
    private bool _isQrSearchActive;
    private bool _pendingInitialCamera;
    private bool _pendingScannedPoiSearch;
    private string _mapModeText = "Chế độ tự do";
    private string _tourName = string.Empty;

    public string MapModeText
    {
        get => _mapModeText;
        set { _mapModeText = value; OnPropertyChanged(nameof(MapModeText)); OnPropertyChanged(nameof(TourHeaderText)); }
    }

    public string TourName
    {
        get => _tourName;
        set
        {
            _tourName = value;
            OnPropertyChanged(nameof(TourName));
            OnPropertyChanged(nameof(TourHeaderText));
            System.Diagnostics.Debug.WriteLine($"[MAP] TourName received: '{value}'");
        }
    }

    public string TourHeaderText
        => !string.IsNullOrWhiteSpace(TourName)
            ? TourName
            : (_isQrSearchActive ? "Kết quả từ QR" : "Địa điểm gần bạn");

    public string HeaderHintText
        => _isQrSearchActive ? "Đang lọc theo POI quét được" : "Chọn tab để lọc";

    public string TourId
    {
        get => _tourId;
        set
        {
            _tourId = value;
            _pendingInitialCamera = true;
            MapModeText = !string.IsNullOrWhiteSpace(value) ? "Chế độ theo tour" : "Chế độ tự do";
            if (!string.IsNullOrWhiteSpace(value))
            {
                _isQrSearchActive = false;
                OnPropertyChanged(nameof(TourHeaderText));
                OnPropertyChanged(nameof(HeaderHintText));
            }
            System.Diagnostics.Debug.WriteLine($"[MAP] TourId received: '{value}'");
        }
    }

    public string ScannedPoiName
    {
        get => _scannedPoiName;
        set
        {
            _scannedPoiName = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_scannedPoiName))
                return;

            _pendingScannedPoiSearch = true;
            _currentCategoryId = 0;
            _pendingInitialCamera = true;
            _isQrSearchActive = true;
            TourName = string.Empty;
            MapModeText = "Chế độ tự do";
            OnPropertyChanged(nameof(TourHeaderText));
            OnPropertyChanged(nameof(HeaderHintText));
            System.Diagnostics.Debug.WriteLine($"[MAP] ScannedPoiName received: '{_scannedPoiName}'");
        }
    }

    // Class dùng để hứng link audio trả về từ Backend
    public class AudioResponse
    {
        public string Url { get; set; }
    }

    public ObservableCollection<PoiModel> SortedPois { get; set; } = new();

    bool _isFirstLocation = true;
    bool _hasFitPoiBounds = false;
    bool isMapExpanded = false;

    // Quản lý ID danh mục hiện tại (0: Tất cả, 1: Tham quan, 2: Ăn uống, 3: Sự kiện)
    int _currentCategoryId = 0;
    private string _searchKeyword = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;

    private PoiModel _currentPlayingPoi;

    // ĐÃ XÓA 2 biến _playbackStartTime và _currentPoiIdTracked vì đã chuyển sang AnalyticsService

    private readonly ApiService _apiService = new ApiService();
    private readonly LocationService _locationService;
    private readonly DeviceMonitorService _deviceMonitorService;
    private readonly IAudioService _audioService;
    private static readonly HttpClient _sharedHttpClient = new HttpClient();
    private readonly ObservableCollection<PoiModel> _visiblePois = new();
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private bool _isLoadingData;
    private string? _lastPoiDrawKey;
    private readonly Dictionary<string, MPoint> _poiProjectionCache = new();
    private Location? _lastUserMapRefreshLocation;
    private DateTime _lastUserMapRefreshTime = DateTime.MinValue;
    // Thay vì gửi mỗi 5 giây, chỉ gửi khi vị trí thay đổi > 30m
    private Location _lastSentLocation;
    private DateTime _lastSentTime = DateTime.MinValue;
    private Location? _lastDistanceUpdateLocation;

    public MapPage(LocationService locationService, DeviceMonitorService deviceMonitorService, IAudioService audioService)
    {
        InitializeComponent();
        BindingContext = this;
        _locationService = locationService;
        _deviceMonitorService = deviceMonitorService;
        _audioService = audioService;
        _audioService.PlaybackStopped += AudioService_PlaybackStopped;

        // Khởi tạo ban đầu
        PoiListView.ItemsSource = _visiblePois;

        InitializeMap();
    }

    private void AudioService_PlaybackStopped(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Chỉ xóa trạng thái nếu Service thực sự đã dừng hẳn (không phải đang chuyển bài)
            if (!_audioService.IsPlaying)
            {
                if (_currentPlayingPoi != null)
                {
                    _currentPlayingPoi.IsPlaying = false;
                    _currentPlayingPoi = null;
                }

                // Reset IsPlaying cho toàn bộ list để chắc chắn
                foreach (var p in _allPois) p.IsPlaying = false;

                _ = AnalyticsService.Instance.TrackAudioStopAsync();
            }
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartTracking();
        _ = _deviceMonitorService.TrackEventAsync("screen_view", new AnalyticsEventDto { Keyword = "map" });
        await LoadDataFromApiAsync();
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _pendingInitialCamera = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTracking();
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = null;
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

    private void StartTracking()
    {
        if (_trackingTask is { IsCompleted: false })
            return;

        _trackingCts = new CancellationTokenSource();
        _trackingTask = TrackLoopAsync(_trackingCts.Token);
    }

    private void StopTracking()
    {
        if (_trackingCts == null) return;
        _trackingCts.Cancel();
        _trackingCts.Dispose();
        _trackingCts = null;
    }

    private async Task TrackLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var location = await _locationService.GetAndSaveCurrentLocationAsync();
                if (location != null)
                {
                    UpdateUserLocationOnMap(location);
                    if (ShouldUpdateDistanceUI(location))
                    {
                        UpdateDistancesAndSort();
                        _lastDistanceUpdateLocation = location;
                    }

                    // Chỉ gửi location_ping khi vị trí thay đổi > 30m HOẶC đã qua 30 giây
                    var distanceChanged = _lastSentLocation == null ||
                        CalculateDistance(location.Latitude, location.Longitude,
                                          _lastSentLocation.Latitude, _lastSentLocation.Longitude) > 30;
                    var timeElapsed = (DateTime.UtcNow - _lastSentTime).TotalSeconds > 30;

                    if (distanceChanged || timeElapsed)
                    {
                        _ = AnalyticsService.Instance.TrackLocationPingAsync(location.Latitude, location.Longitude);
                        _lastSentLocation = location;
                        _lastSentTime = DateTime.UtcNow;
                    }
                }
            }
            catch { }

            try
            {
                await Task.Delay(5000, cancellationToken); // vẫn check vị trí mỗi 5 giây, nhưng chỉ gửi khi cần
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    async Task LoadDataFromApiAsync()
    {
        if (_isLoadingData) return;
        _isLoadingData = true;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[MAP] Load start. TourId='{TourId}', TourName='{TourName}'");

            if (!string.IsNullOrWhiteSpace(TourId))
            {
                var tours = await _apiService.GetToursAsync() ?? new List<TourModel>();
                System.Diagnostics.Debug.WriteLine($"[MAP] Tours loaded: {tours.Count}");

                var selectedTour = tours.FirstOrDefault(t => string.Equals(t.Id, TourId, StringComparison.OrdinalIgnoreCase));
                System.Diagnostics.Debug.WriteLine($"[MAP] SelectedTour found: {(selectedTour != null ? selectedTour.Name : "null")}");

                if (selectedTour?.Pois != null && selectedTour.Pois.Count > 0)
                {
                    _allPois = selectedTour.Pois.Where(p => p != null).ToList();
                    TourName = selectedTour.Name;
                    MapModeText = "Chế độ theo tour";
                    System.Diagnostics.Debug.WriteLine($"[MAP] Tour mode from API tour. TourId={TourId}, TourName={selectedTour.Name}, POIs={_allPois.Count}");
                    foreach (var p in _allPois)
                        System.Diagnostics.Debug.WriteLine($"[MAP] POI {p.Id} {p.Name} lat={p.Lat} lng={p.Lng} active={p.IsPlaying}");
                }
                else
                {
                    _allPois = new List<PoiModel>();
                    TourName = "Không tìm thấy tour";
                    MapModeText = "Chế độ theo tour";
                    System.Diagnostics.Debug.WriteLine($"[MAP] Tour mode but tour not found or no POIs. TourId={TourId}");
                }
            }
            else
            {
                _allPois = await _apiService.GetAllLocationsAsync() ?? new List<PoiModel>();
                MapModeText = "Chế độ tự do";
                TourName = string.Empty;
                System.Diagnostics.Debug.WriteLine($"[MAP] Free mode. POIs={_allPois.Count}");
            }

            _poiProjectionCache.Clear();
            _lastPoiDrawKey = null;
            GenerateCategoryTabs();
            UpdateDistancesAndSort();
            DrawPoisOnMap();
            ApplyInitialCamera();
            ApplyScannedPoiSearchIfNeeded();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi tải POIs: {ex}");
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    void DrawPoisOnMap()
    {
        if (mapView == null || _allPois == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var filteredPois = GetFilteredPois().ToList();

            var poiFeatures = new List<IFeature>();
            var validMapPoints = new List<MPoint>();
            var drawKey = BuildPoiDrawKey(filteredPois);

            if (string.Equals(_lastPoiDrawKey, drawKey, StringComparison.Ordinal))
            {
                return;
            }
            _lastPoiDrawKey = drawKey;

            var showLabels = filteredPois.Count <= 40;

            foreach (var poi in filteredPois)
            {
                if (!IsValidCoordinate(poi.Lat, poi.Lng))
                    continue;

                var mapPoint = GetProjectedPoint(poi);
                var feature = new PointFeature(mapPoint);

                feature.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    Fill = new Mapsui.Styles.Brush(poi.PinColor),
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2),
                    SymbolScale = 1.0
                });

                if (showLabels)
                {
                    feature.Styles.Add(new LabelStyle
                    {
                        Text = poi.Name,
                        Offset = new Offset(0, -20),
                        ForeColor = Mapsui.Styles.Color.Black,
                        BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White),
                        Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
                    });
                }

                poiFeatures.Add(feature);
                validMapPoints.Add(mapPoint);
            }

            _poiLayer.Features = poiFeatures;
            _poiLayer.DataHasChanged();
            mapView.RefreshGraphics();

            if (!_hasFitPoiBounds && validMapPoints.Count > 0)
            {
                FitMapToPoiBounds(validMapPoints);
                _hasFitPoiBounds = true;
            }
        });
    }

    private string BuildPoiDrawKey(List<PoiModel> pois)
    {
        var selectedId = pois.FirstOrDefault(p => p.IsSelected)?.Id.ToString() ?? "none";
        var ids = string.Join(',', pois.Where(p => p != null).Select(p => p.Id));
        return $"{_currentCategoryId}|{_searchKeyword}|{selectedId}|{ids}";
    }

    private MPoint GetProjectedPoint(PoiModel poi)
    {
        var cacheKey = $"{poi.Id}:{poi.Lat:F6}:{poi.Lng:F6}";
        if (_poiProjectionCache.TryGetValue(cacheKey, out var cachedPoint))
            return cachedPoint;

        var proj = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
        var point = new MPoint(proj.x, proj.y);
        _poiProjectionCache[cacheKey] = point;
        return point;
    }

    private void UpdateUserLocationOnMap(Microsoft.Maui.Devices.Sensors.Location location)
    {
        if (mapView == null) return;
        if (!ShouldRefreshUserMarker(location)) return;

        var proj = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
        var mapPoint = new MPoint(proj.x, proj.y);
        var userFeature = new PointFeature(mapPoint);
        userFeature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3), SymbolScale = 0.8 });
        _userLocationLayer.Features = new List<IFeature> { userFeature };
        _userLocationLayer.DataHasChanged();
        MainThread.BeginInvokeOnMainThread(() => mapView?.RefreshGraphics());

        _lastUserMapRefreshLocation = location;
        _lastUserMapRefreshTime = DateTime.UtcNow;
    }

    private static bool IsValidCoordinate(double lat, double lng)
        => lat is >= -90 and <= 90 && lng is >= -180 and <= 180 && !(lat == 0 && lng == 0);

    private void FitMapToPoiBounds(List<MPoint> points)
    {
        if (mapView?.Map?.Navigator == null || points == null || points.Count == 0)
            return;

        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);

        var center = new MPoint((minX + maxX) / 2, (minY + maxY) / 2);

        if (Math.Abs(maxX - minX) < 1 && Math.Abs(maxY - minY) < 1)
        {
            mapView.Map.Navigator.CenterOn(points[0]);
            mapView.Map.Navigator.ZoomTo(14);
        }
        else
        {
            mapView.Map.Navigator.CenterOn(center);
            mapView.Map.Navigator.ZoomTo(12);
        }
    }

    private void ApplyInitialCamera()
    {
        if (mapView?.Map?.Navigator == null || !_pendingInitialCamera)
            return;

        _pendingInitialCamera = false;

        if (_allPois != null && _allPois.Count > 0)
        {
            var points = _allPois
                .Where(p => IsValidCoordinate(p.Lat, p.Lng))
                .Select(GetProjectedPoint)
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[MAP] ApplyInitialCamera points={points.Count}");

            if (points.Count > 0)
            {
                FitMapToPoiBounds(points);
                return;
            }
        }

        var userLoc = _locationService.CurrentLocation;
        if (userLoc != null)
        {
            var proj = SphericalMercator.FromLonLat(userLoc.Longitude, userLoc.Latitude);
            mapView.Map.Navigator.CenterOn(new MPoint(proj.x, proj.y));
            mapView.Map.Navigator.ZoomTo(14);
            return;
        }

        // Vị trí mặc định của trung tâm thành phố nếu chưa có GPS
        var cityCenter = SphericalMercator.FromLonLat(106.6297, 10.8231);
        mapView.Map.Navigator.CenterOn(new MPoint(cityCenter.x, cityCenter.y));
        mapView.Map.Navigator.ZoomTo(12);
    }

    private static bool MatchesTour(PoiModel poi, string tourId)
    {
        if (poi == null || string.IsNullOrWhiteSpace(tourId)) return false;
        return string.Equals(poi.Id.ToString(), tourId, StringComparison.OrdinalIgnoreCase)
               || string.Equals(poi.NarrationId.ToString(), tourId, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateDistancesAndSort()
    {
        var userLoc = _locationService.CurrentLocation;
        if (_allPois == null) return;

        if (userLoc != null)
        {
            foreach (var poi in _allPois)
            {
                double dist = CalculateDistance(userLoc.Latitude, userLoc.Longitude, poi.Lat, poi.Lng);
                poi.DistanceMeters = dist;
            }
        }

        var filtered = GetFilteredPois()
            .OrderByDescending(p => p.IsSelected)
            .ThenBy(p => p.DistanceMeters)
            .ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _visiblePois.Clear();
            foreach (var poi in filtered)
            {
                _visiblePois.Add(poi);
            }
        });
    }

    private bool ShouldUpdateDistanceUI(Location currentLocation)
    {
        if (_lastDistanceUpdateLocation == null) return true;
        var movedMeters = CalculateDistance(
            currentLocation.Latitude,
            currentLocation.Longitude,
            _lastDistanceUpdateLocation.Latitude,
            _lastDistanceUpdateLocation.Longitude);
        return movedMeters > 8;
    }

    private bool ShouldRefreshUserMarker(Location currentLocation)
    {
        if (_lastUserMapRefreshLocation == null) return true;

        var movedMeters = CalculateDistance(
            currentLocation.Latitude,
            currentLocation.Longitude,
            _lastUserMapRefreshLocation.Latitude,
            _lastUserMapRefreshLocation.Longitude);

        var elapsedMs = (DateTime.UtcNow - _lastUserMapRefreshTime).TotalMilliseconds;
        return movedMeters > 3 || elapsedMs > 1500;
    }

    private IEnumerable<PoiModel> GetFilteredPois()
    {
        IEnumerable<PoiModel> query = _currentCategoryId == 0
            ? _allPois
            : _allPois.Where(p => p.CategoryId == _currentCategoryId);

        if (string.IsNullOrWhiteSpace(_searchKeyword))
            return query;

        var keyword = _searchKeyword.Trim();
        return query.Where(p =>
            (!string.IsNullOrWhiteSpace(p.Name) && p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(p.Description) && p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(p.Address) && p.Address.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
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
    // TẠO TAB DANH MỤC CỐ ĐỊNH & ĐỔI MÀU NÚT BẤM
    // ==========================================
    private string GetCategoryColorHex(int categoryId) => categoryId switch
    {
        1 => "#E63946",
        2 => "#F4A261",
        3 => "#6A4C93",
        _ => "#2A9D8F"
    };

    private void GenerateCategoryTabs()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CategoryTabsContainer.Children.Clear();

            CategoryTabsContainer.Children.Add(CreateTabButton(0, "Tất cả"));
            CategoryTabsContainer.Children.Add(CreateTabButton(1, "Tham quan"));
            CategoryTabsContainer.Children.Add(CreateTabButton(2, "Ăn uống"));
            CategoryTabsContainer.Children.Add(CreateTabButton(3, "Sự kiện"));
        });
    }

    private Button CreateTabButton(int categoryId, string categoryName)
    {
        bool isSelected = _currentCategoryId == categoryId;
        string activeColorHex = GetCategoryColorHex(categoryId);

        var btn = new Button
        {
            Text = categoryName,
            CommandParameter = categoryId,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 10,
            FontSize = 12,
            HeightRequest = 35,
            Padding = new Thickness(15, 0),
            BackgroundColor = isSelected ? Microsoft.Maui.Graphics.Color.FromArgb(activeColorHex) : Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3"),
            TextColor = isSelected ? Microsoft.Maui.Graphics.Colors.White : Microsoft.Maui.Graphics.Colors.DimGray
        };

        btn.Clicked += DynamicTab_Clicked;
        return btn;
    }

    private void DynamicTab_Clicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn == null) return;

        _currentCategoryId = (int)btn.CommandParameter;
        if (string.IsNullOrWhiteSpace(TourId))
        {
            MapModeText = "Chế độ tự do";
            TourName = string.Empty;
        }

        foreach (var child in CategoryTabsContainer.Children)
        {
            if (child is Button b)
            {
                int currentBtnId = (int)b.CommandParameter;
                bool isSelected = currentBtnId == _currentCategoryId;
                string activeColorHex = GetCategoryColorHex(currentBtnId);

                b.BackgroundColor = isSelected ? Microsoft.Maui.Graphics.Color.FromArgb(activeColorHex) : Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3");
                b.TextColor = isSelected ? Microsoft.Maui.Graphics.Colors.White : Microsoft.Maui.Graphics.Colors.DimGray;
            }
        }

        UpdateDistancesAndSort();
        DrawPoisOnMap();
    }

    // ==========================================
    // XỬ LÝ SỰ KIỆN CLICK CỦA NGƯỜI DÙNG
    // ==========================================
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

    private async void Back_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private void Search_Clicked(object sender, EventArgs e)
    {
        _searchKeyword = txtSearch.Text?.Trim() ?? string.Empty;
        UpdateQrHeaderStateBySearchKeyword();
        UpdateDistancesAndSort();
        DrawPoisOnMap();

        var firstPoi = _visiblePois.FirstOrDefault();
        FocusOnPoi(firstPoi);
    }

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var token = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(250, token);
            _searchKeyword = e.NewTextValue?.Trim() ?? string.Empty;
            UpdateQrHeaderStateBySearchKeyword();
            UpdateDistancesAndSort();
            DrawPoisOnMap();
        }
        catch (OperationCanceledException)
        {
            // Người dùng tiếp tục gõ, bỏ lần debounce cũ
        }
    }

    private void BtnViewMap_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null && mapView != null)
        {
            MarkSelectedPoi(poi);
            var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
            mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
            mapView.Map.Navigator.ZoomTo(15);

            // TRACKING: Xem bản đồ
            _ = AnalyticsService.Instance.TrackPoiViewAsync(poi.Id);

            PoiDetailPopupView.HidePopup();
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
            MarkSelectedPoi(poi);
            PoiDetailPopupView.ShowPopup(poi);
            _ = AnalyticsService.Instance.TrackPoiViewAsync(poi.Id);
        }
    }

    private void ClosePopup_Clicked(object sender, EventArgs e)
    {
        PoiDetailPopupView.HidePopup();
        ClearSelectedPoi();
    }

    private void MarkSelectedPoi(PoiModel poi)
    {
        foreach (var item in _allPois)
            item.IsSelected = false;

        poi.IsSelected = true;
        UpdateDistancesAndSort();
        DrawPoisOnMap();
    }

    private void ClearSelectedPoi()
    {
        foreach (var item in _allPois)
            item.IsSelected = false;

        UpdateDistancesAndSort();
        DrawPoisOnMap();
    }

    private void ApplyScannedPoiSearchIfNeeded()
    {
        if (!_pendingScannedPoiSearch || string.IsNullOrWhiteSpace(_scannedPoiName))
            return;

        _pendingScannedPoiSearch = false;
        _searchKeyword = _scannedPoiName.Trim();
        txtSearch.Text = _searchKeyword;

        UpdateDistancesAndSort();
        DrawPoisOnMap();

        var exactPoi = _visiblePois.FirstOrDefault(p =>
            string.Equals(p.Name, _searchKeyword, StringComparison.OrdinalIgnoreCase));
        var targetPoi = exactPoi ?? _visiblePois.FirstOrDefault();
        if (targetPoi == null) return;

        MarkSelectedPoi(targetPoi);
        FocusOnPoi(targetPoi);
    }

    private void UpdateQrHeaderStateBySearchKeyword()
    {
        if (_isQrSearchActive && string.IsNullOrWhiteSpace(_searchKeyword))
        {
            _isQrSearchActive = false;
            OnPropertyChanged(nameof(TourHeaderText));
            OnPropertyChanged(nameof(HeaderHintText));
        }
    }

    private void FocusOnPoi(PoiModel? poi)
    {
        if (poi == null || mapView == null || !IsValidCoordinate(poi.Lat, poi.Lng))
            return;

        var projected = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
        mapView.Map.Navigator.CenterOn(new MPoint(projected.x, projected.y));
        mapView.Map.Navigator.ZoomTo(15);
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

        // Geofencing
        var userLoc = _locationService.CurrentLocation;
        bool isOnSite = false;

        if (userLoc != null)
        {
            if (poi.DistanceMeters > poi.Radius)
            {
                bool confirm = await DisplayAlert("Bạn đang ở xa",
                    $"Bạn cách {poi.Name} khoảng {Math.Round(poi.DistanceMeters)}m. Bạn có muốn nghe thuyết minh ảo từ xa không?",
                    "Nghe", "Hủy bỏ");
                if (!confirm) return;
            }
            else
            {
                isOnSite = true;
            }
        }

        // Dừng cái cũ trước khi gán cái mới
        await _audioService.StopAsync();

        poi.IsPlaying = true;
        _currentPlayingPoi = poi;

        // Gửi Tracking: Báo server là bắt đầu nghe
        _ = AnalyticsService.Instance.TrackAudioStartAsync(poi.Id, poi.LanguageCode ?? "vi", isOnSite);

        try
        {
            if (!string.IsNullOrEmpty(poi.AudioUrl))
            {
                string finalUrl = FixLocalhostUrl(poi.AudioUrl);
                await _audioService.PlayAudioAsync(finalUrl);
            }
            else
            {
                await ReadTextOffline(poi);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi gọi API âm thanh: {ex.Message}");
            await ReadTextOffline(poi);
        }
    }

    private async void StopPlayback(PoiModel poi)
    {
        poi.IsPlaying = false;
        await _audioService.StopAsync();

        if (_currentPlayingPoi == poi) _currentPlayingPoi = null;

        // Gửi Tracking: Báo server là kết thúc nghe
        _ = AnalyticsService.Instance.TrackAudioStopAsync();
    }


    // ĐÃ XÓA hàm SendListenDurationTracking() cũ đi

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
        string content = string.IsNullOrWhiteSpace(poi.FullContent) ? poi.Description : poi.FullContent;
        if (string.IsNullOrWhiteSpace(content)) content = "Không có thông tin thuyết minh.";

        string langCode = Preferences.Default.Get("UserLanguage", poi.LanguageCode ?? "vi");
        await _audioService.PlayTextToSpeechAsync($"{poi.Name}. {content}", langCode);
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ SỰ KIỆN THIẾU (THEO YÊU CẦU)
    // ==========================================
    private void BottomSheet_PanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // Xử lý gesture kéo cho bottom sheet
        // Implementation tự theo yêu cầu UI
    }

    private void PoiDetailPopupView_SpeakRequested(object sender, PoiModel? poi)
    {
        if (poi == null) return;
        var mockButton = new Button { CommandParameter = poi };
        BtnSpeak_Clicked(mockButton, EventArgs.Empty);
    }

    private void PoiDetailPopupView_ViewMapRequested(object sender, PoiModel? poi)
    {
        if (poi == null) return;

        MarkSelectedPoi(poi);

        if (mapView == null)
            return;

        var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
        mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
        mapView.Map.Navigator.ZoomTo(15);

        PoiDetailPopupView.HidePopup();
        InfoPanel.IsVisible = true;
        if (isMapExpanded)
        {
            Grid.SetRowSpan(MapSection, 1);
            InfoPanel.IsVisible = true;
            BtnToggleMap.Text = "Mở rộng";
            isMapExpanded = false;
        }
    }

    private void ToggleBottomSheet_Clicked(object? sender, EventArgs e)
    {
        // Xử lý toggle bottom sheet
        InfoPanel.IsVisible = !InfoPanel.IsVisible;
    }

    private void ToggleMapType_Clicked(object? sender, EventArgs e)
    {
        // Xử lý chuyển đổi loại bản đồ
        // Implementation tự theo yêu cầu
    }

    private void ZoomIn_Clicked(object? sender, EventArgs e)
    {
        // Xử lý zoom vào bản đồ
        if (mapView != null)
        {
            mapView.Map.Navigator.ZoomIn();
        }
    }

    private void ZoomOut_Clicked(object? sender, EventArgs e)
    {
        // Xử lý zoom ra bản đồ
        if (mapView != null)
        {
            mapView.Map.Navigator.ZoomOut();
        }
    }
}