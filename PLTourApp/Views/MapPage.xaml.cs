using Mapsui;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices.Sensors;
using PLTourApp.Database;
using PLTourApp.Engines;
using PLTourApp.Models;
using PLTourApp.Services;
using PLTourApp.ViewModels;

// Alias để phân biệt màu của MAUI và Mapsui
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace PLTourApp.Views;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel viewModel;
    private readonly SQLiteHelper db;
    private readonly LocationService locationService;
    private Poi selectedPoi;

    public MapPage()
    {
        InitializeComponent();
        db = new SQLiteHelper();
        viewModel = new MapViewModel(db);
        locationService = new LocationService();
        locationService.LocationChanged += OnLocationChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await InitMap();
    }

    private async Task InitMap()
    {
        try
        {
            var map = new Mapsui.Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());

            await viewModel.LoadPois();
            var features = new List<IFeature>();

            foreach (var poi in viewModel.Pois)
            {
                var position = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
                var feature = new PointFeature(new MPoint(position.x, position.y));

                // GIẢI PHÁP TRIỆT ĐỂ CHO LỖI BRUSH/COLOR
                feature.Styles.Add(new SymbolStyle
                {
                    SymbolScale = 0.8,
                    // Mapsui v4/v5: Fill = new Brush(new Color(...))
                    Fill = new Mapsui.Styles.Brush
                    {
                        Color = new Mapsui.Styles.Color(255, 140, 107)
                    }
                });

                features.Add(feature);
            }

            map.Layers.Add(new MemoryLayer
            {
                Name = "POI Layer",
                Features = features
            });

            MyMap.Map = map;

            var center = SphericalMercator.FromLonLat(108.2022, 16.0544);
            map.Navigator.CenterOn(new MPoint(center.x, center.y));
            map.Navigator.ZoomTo(2);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }

    private void OnLocationChanged(Location location)
    {
        var nearest = viewModel.FindNearest(location);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (nearest == null) return;
            selectedPoi = nearest;
            NearestPoiLabel.Text = nearest.Name;
        });
    }

    private async void OnLocateClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync()
                           ?? await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

            if (location != null)
            {
                var userPos = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                MyMap.Map.Navigator.CenterOn(new MPoint(userPos.x, userPos.y));
                MyMap.Map.Navigator.ZoomTo(1.5);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể định vị: " + ex.Message, "OK");
        }
    }

    private async void OnViewDetailClicked(object sender, EventArgs e)
    {
        if (selectedPoi == null)
        {
            await DisplayAlert("Thông báo", "Chưa chọn địa điểm", "OK");
            return;
        }

        var engine = new NarrationEngine(new AudioService(), new TTSService(), db);
        await Navigation.PushAsync(new PoiDetailPage(selectedPoi, engine));
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        locationService.LocationChanged -= OnLocationChanged;
    }
}