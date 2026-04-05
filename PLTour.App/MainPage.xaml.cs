#nullable disable
using Mapsui;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Mapsui.Layers;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

// ⚠️ ĐẢM BẢO TÊN NÀY PHẢI TRÙNG VỚI TÊN PROJECT CỦA BẠN
namespace PLTour.App;

// LỚP DỮ LIỆU CHỨA THÔNG TIN ĐỊA ĐIỂM
public class PoiModel : INotifyPropertyChanged
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Category { get; set; }
    public string Address { get; set; }
    public Mapsui.Styles.Color PinColor { get; set; }
    public Microsoft.Maui.Graphics.Color CategoryColor { get; set; }

    // ✅ Đã đổi từ DistanceKm thành DistanceMeters để giữ độ chuẩn xác
    private double _distanceMeters;
    public double DistanceMeters
    {
        get => _distanceMeters;
        set { _distanceMeters = value; OnPropertyChanged(nameof(DistanceText)); }
    }

    // ✅ Logic tự động: < 1000 thì hiện m, >= 1000 thì hiện km
    public string DistanceText
    {
        get
        {
            if (DistanceMeters <= 0) return "Đang đo...";
            if (DistanceMeters < 1000)
                return $"{Math.Round(DistanceMeters)} m"; // Hiện mét không lấy số thập phân
            else
                return $"{(DistanceMeters / 1000.0):F1} km"; // Hiện km lấy 1 số thập phân
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

// LỚP XỬ LÝ GIAO DIỆN CHÍNH
public partial class MainPage : ContentPage
{
    MapView mapView;
    MemoryLayer _userLocationLayer;
    MemoryLayer _poiLayer;

    IEnumerable<Locale> _locales;
    Locale _selectedLocale;

    List<PoiModel> _allPois;
    public ObservableCollection<PoiModel> DisplayedPois { get; set; } = new ObservableCollection<PoiModel>();

    string lastSpokenPoi = "";
    DateTime lastTriggered = DateTime.MinValue;
    bool isSpeaking = false;
    bool _isFirstLocation = true;
    double _currentLat = 0;
    double _currentLng = 0;

    public MainPage()
    {
        InitializeComponent();
        InitializeData();
        PoiListView.ItemsSource = DisplayedPois;

        mapView = new MapView();
        mapView.Map = new Mapsui.Map();

        // 🔥 ĐÃ THÊM: Xóa các widget thông báo rác (FPS, Info) của Mapsui trên bản đồ
        mapView.Map.Widgets.Clear();

        mapView.Map.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());

        _userLocationLayer = new MemoryLayer { Name = "User" };
        mapView.Map.Layers.Add(_userLocationLayer);

        _poiLayer = new MemoryLayer { Name = "POIs" };
        DrawPoisOnMap();
        mapView.Map.Layers.Add(_poiLayer);

        MainGrid.Children.Insert(0, mapView);

        FilterListByCategory("Tham quan");
        _ = LoadLanguagesAsync();
        StartTracking();
    }

    void InitializeData()
    {
        _allPois = new List<PoiModel>
        {
            new PoiModel { Lat = 10.821335, Lng = 106.600329, Radius = 200, Name = "Vị trí xuất phát", Address = "123 Lê Trọng Tấn", Description = "Bạn đang ở gần vị trí xuất phát.", Category = "Tham quan", PinColor = Mapsui.Styles.Color.Green, CategoryColor = Microsoft.Maui.Graphics.Colors.Teal },
            new PoiModel { Lat = 10.816229, Lng = 106.601858, Radius = 150, Name = "Nhà sách Bình Tân", Address = "202 Lê Trọng Tấn", Description = "Nơi cung cấp hàng ngàn đầu sách đa dạng.", Category = "Tham quan", PinColor = Mapsui.Styles.Color.Green, CategoryColor = Microsoft.Maui.Graphics.Colors.Teal },
            new PoiModel { Lat = 10.822000, Lng = 106.601000, Radius = 150, Name = "Cà phê Gió Mới", Address = "45 Nguyễn Hữu Tiến", Description = "Không gian yên tĩnh, cà phê nguyên chất.", Category = "Ăn uống", PinColor = Mapsui.Styles.Color.Orange, CategoryColor = Microsoft.Maui.Graphics.Colors.Orange },
            new PoiModel { Lat = 10.820000, Lng = 106.599000, Radius = 150, Name = "Bánh tráng nướng", Address = "Vỉa hè KCN Tân Bình", Description = "Đặc sản bánh tráng nướng siêu ngon.", Category = "Ăn uống", PinColor = Mapsui.Styles.Color.Orange, CategoryColor = Microsoft.Maui.Graphics.Colors.Orange },
            new PoiModel { Lat = 10.818000, Lng = 106.605000, Radius = 200, Name = "Hội chợ Đêm", Address = "Công viên CELADON", Description = "Hội chợ mua sắm cuối tuần náo nhiệt.", Category = "Giải trí / Sự kiện", PinColor = Mapsui.Styles.Color.Purple, CategoryColor = Microsoft.Maui.Graphics.Colors.Purple }
        };
    }

    void DrawPoisOnMap()
    {
        var poiFeatures = new List<IFeature>();
        foreach (var poi in _allPois)
        {
            var proj = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
            var mapPoint = new MPoint(proj.x, proj.y);
            var feature = new PointFeature(mapPoint);

            feature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 255, 255, 180)), SymbolScale = 0.8 });
            feature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(poi.PinColor), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2), SymbolScale = 0.5 });
            feature.Styles.Add(new LabelStyle { Text = poi.Name, Offset = new Offset(0, -20), ForeColor = Mapsui.Styles.Color.Black, BackColor = new Mapsui.Styles.Brush(Mapsui.Styles.Color.White), Halo = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2) });
            poiFeatures.Add(feature);
        }
        _poiLayer.Features = poiFeatures;
    }

    private void Tab_Clicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        string category = btn.CommandParameter.ToString();

        TabThamQuan.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3"); TabThamQuan.TextColor = Microsoft.Maui.Graphics.Colors.DimGray;
        TabAnUong.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3"); TabAnUong.TextColor = Microsoft.Maui.Graphics.Colors.DimGray;
        TabSuKien.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#D3D3D3"); TabSuKien.TextColor = Microsoft.Maui.Graphics.Colors.DimGray;

        btn.TextColor = Microsoft.Maui.Graphics.Colors.White;
        if (category == "Tham quan") btn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2A9D8F");
        else if (category == "Ăn uống") btn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F4A261");
        else if (category == "Giải trí / Sự kiện") btn.BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#9D4EDD");

        FilterListByCategory(category);
    }

    void FilterListByCategory(string category)
    {
        DisplayedPois.Clear();
        var filtered = _allPois.Where(p => p.Category == category).ToList();
        // ✅ Cập nhật sắp xếp theo DistanceMeters thay vì DistanceKm
        if (_currentLat != 0) filtered = filtered.OrderBy(p => p.DistanceMeters).ToList();
        foreach (var item in filtered) DisplayedPois.Add(item);
    }

    private void BtnViewMap_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null)
        {
            var p = SphericalMercator.FromLonLat(poi.Lng, poi.Lat);
            mapView.Map.Navigator.CenterOn(new MPoint(p.x, p.y));
            mapView.Map.Navigator.ZoomTo(1.5);
        }
    }

    private void BtnSpeak_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null) SpeakText($"{poi.Name}. {poi.Description}");
    }

    private async void BtnRoute_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null)
        {
            var location = new Location(poi.Lat, poi.Lng);
            var options = new MapLaunchOptions { Name = poi.Name, NavigationMode = NavigationMode.Driving };
            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location, options);
        }
    }

    async void StartTracking()
    {
        var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted) return;

        while (true)
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));
                if (location == null) { await Task.Delay(5000); continue; }

                _currentLat = location.Latitude;
                _currentLng = location.Longitude;

                var proj = SphericalMercator.FromLonLat(_currentLng, _currentLat);
                var mapPoint = new MPoint(proj.x, proj.y);

                if (_isFirstLocation) { mapView.Map?.Navigator.CenterOn(mapPoint); mapView.Map?.Navigator.ZoomTo(2); _isFirstLocation = false; }

                var userFeature = new PointFeature(mapPoint);
                userFeature.Styles.Add(new SymbolStyle { SymbolType = SymbolType.Ellipse, Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.Blue), Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3), SymbolScale = 0.6 });
                _userLocationLayer.Features = new List<IFeature> { userFeature };
                _userLocationLayer.DataHasChanged();

                MainThread.BeginInvokeOnMainThread(() => {
                    // 🔥 ĐÃ THÊM: Ẩn thông báo "Đang tìm vị trí..." khi đã có GPS
                    if (LoadingOverlay != null && LoadingOverlay.IsVisible)
                    {
                        LoadingOverlay.IsVisible = false;
                    }

                    lblInfo.Text = $"Vị trí: {_currentLat:F4}, {_currentLng:F4}";
                    mapView.RefreshGraphics();
                });

                foreach (var poi in _allPois)
                {
                    double distMeters = CalculateDistance(_currentLat, _currentLng, poi.Lat, poi.Lng);

                    // ✅ Gán trực tiếp số mét vào model, model sẽ tự biết lúc nào hiện m, lúc nào hiện km
                    poi.DistanceMeters = distMeters;

                    if (distMeters <= poi.Radius && lastSpokenPoi != poi.Name && !isSpeaking)
                    {
                        lastSpokenPoi = poi.Name;
                        SpeakText($"{poi.Name}. {poi.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    lblInfo.Text = $"Lỗi GPS: {ex.Message}";
                });
            }
            await Task.Delay(5000);
        }
    }

    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    async Task LoadLanguagesAsync()
    {
        try
        {
            _locales = await TextToSpeech.GetLocalesAsync();
            var languageList = new List<string>();
            int viIndex = 0, currentIndex = 0;
            foreach (var locale in _locales)
            {
                languageList.Add($"{locale.Name} ({locale.Country})");
                if (locale.Language.ToLower().Contains("vi") || locale.Name.ToLower().Contains("viet")) viIndex = currentIndex;
                currentIndex++;
            }
            MainThread.BeginInvokeOnMainThread(() => { LanguagePicker.ItemsSource = languageList; if (languageList.Count > 0) { LanguagePicker.SelectedIndex = viIndex; LanguagePicker.Title = "Chọn giọng đọc"; } });
        }
        catch { }
    }
    private void LanguagePicker_SelectedIndexChanged(object sender, EventArgs e) { if (LanguagePicker.SelectedIndex >= 0 && _locales != null) _selectedLocale = _locales.ElementAt(LanguagePicker.SelectedIndex); }
    private async void SpeakText(string text) { if (isSpeaking) return; isSpeaking = true; try { await TextToSpeech.SpeakAsync(text, new SpeechOptions { Locale = _selectedLocale }); } catch { } finally { isSpeaking = false; } }
}