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

namespace PLTour.App.Pages;

public partial class MapPage : ContentPage
{
    MapView mapView;
    MemoryLayer _userLocationLayer;
    MemoryLayer _poiLayer;
    List<PoiModel> _allPois = new List<PoiModel>();
    public ObservableCollection<PoiModel> SortedPois { get; set; } = new();

    bool isSpeaking = false;
    bool _isFirstLocation = true;
    bool isMapExpanded = false;
    string _currentCategory = "Tất cả";

    private readonly ApiService _apiService = new ApiService();
    private readonly LocationService _locationService;

    public MapPage(LocationService locationService)
    {
        InitializeComponent();
        _locationService = locationService;
        PoiListView.ItemsSource = SortedPois;

        InitializeMap();
        StartTracking();
        _ = LoadDataFromApiAsync();
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

    private void UpdateUserLocationOnMap(Location location)
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
            : _allPois.Where(p => p.Category == _currentCategory).OrderBy(p => p.DistanceMeters).ToList();

        MainThread.BeginInvokeOnMainThread(() => {
            if (SortedPois.Count == 0 || SortedPois[0].Name != filtered[0].Name)
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
        if (isMapExpanded)
        {
            Grid.SetRowSpan(MapSection, 2);
            InfoPanel.IsVisible = false;
            BtnToggleMap.Text = "Small 📂";
        }
        else
        {
            Grid.SetRowSpan(MapSection, 1);
            InfoPanel.IsVisible = true;
            BtnToggleMap.Text = "Full 🔲";
        }
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

    async Task LoadDataFromApiAsync()
    {
        var tours = await _apiService.GetMockToursAsync();
        _allPois.Clear();
        foreach (var tour in tours) if (tour.Pois != null) _allPois.AddRange(tour.Pois);
        MainThread.BeginInvokeOnMainThread(() => { DrawPoisOnMap(); UpdateDistancesAndSort(); });
    }

    void DrawPoisOnMap()
    {
        if (mapView == null) return;
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

    private async void BtnSpeak_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null && !isSpeaking)
        {
            isSpeaking = true;
            try { await TextToSpeech.SpeakAsync($"{poi.Name}. {poi.Description}"); } finally { isSpeaking = false; }
        }
    }

    private async void BtnRoute_Clicked(object sender, EventArgs e)
    {
        var poi = (sender as Button)?.CommandParameter as PoiModel;
        if (poi != null) await Microsoft.Maui.ApplicationModel.Map.OpenAsync(new Location(poi.Lat, poi.Lng), new MapLaunchOptions { Name = poi.Name, NavigationMode = NavigationMode.Driving });
    }

    // Đã xóa phần Load giọng nói vì sẽ chuyển sang trang Setting
}