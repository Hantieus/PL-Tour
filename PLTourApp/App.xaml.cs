using PLTourApp.Database;
using PLTourApp.Engines;
using PLTourApp.Services;

namespace PLTourApp;

public partial class App : Application
{
    SQLiteHelper db;
    LocationManager manager;
    NarrationEngine narration;

    public App(
        SQLiteHelper database,
        LocationManager locationManager,
        NarrationEngine narrationEngine,
        LocationService locationService)
    {
        InitializeComponent();

        db = database;
        manager = locationManager;
        narration = narrationEngine;

        manager.PoiTriggered += async (poi, location) =>
        {
            await narration.Enqueue(poi);
        };

        InitApp(locationService);

        MainPage = new AppShell();
    }

    async void InitApp(LocationService locationService)
    {
        try
        {
            await db.Init();
            await SeedData.Seed(db);
            await manager.Init();
            await locationService.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"INIT ERROR: {ex.Message}");
        }
    }
}