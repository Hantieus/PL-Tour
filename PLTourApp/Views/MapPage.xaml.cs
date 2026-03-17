using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using PLTourApp.Database;
using PLTourApp.Services;
using PLTourApp.ViewModels;

namespace PLTourApp.Views;

public partial class MapPage : ContentPage
{
    MapViewModel viewModel;
    LocationService locationService;
    SQLiteHelper db;

    public MapPage()
    {
        InitializeComponent();

        db = new SQLiteHelper();

        viewModel = new MapViewModel(db);

        locationService = new LocationService();

        locationService.LocationChanged += OnLocationChanged;

        LoadMap();
    }

    async void LoadMap()
    {
        await viewModel.LoadPois();

        foreach (var poi in viewModel.Pois)
        {
            var pin = new Pin
            {
                Label = poi.Name,
                Location = new Location(poi.Latitude, poi.Longitude),
                Type = PinType.Place
            };

            TourMap.Pins.Add(pin);
        }
    }

    void OnLocationChanged(Location location)
    {
        var nearest = viewModel.FindNearest(location);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (nearest != null)
            {
                NearestPoiLabel.Text = nearest.Name;
            }
        });
    }
}