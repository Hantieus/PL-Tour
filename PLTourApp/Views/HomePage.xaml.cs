using PLTourApp.ViewModels;
using PLTourApp.Services;
using PLTourApp.Database;
using PLTourApp.Engines;

namespace PLTourApp.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    async void OnMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage());
    }

    async void OnPoiClicked(object sender, EventArgs e)
    {
        var db = new SQLiteHelper();
        var vm = new MapViewModel(db);

        await Navigation.PushAsync(new PoiListPage(vm));
    }

    async void OnQRClicked(object sender, EventArgs e)
    {
        var db = new SQLiteHelper();

        var audio = new AudioService();
        var tts = new TTSService();

        var narration = new NarrationEngine(audio, tts, db);

        await Navigation.PushAsync(new QRScannerPage(db, narration));
    }

    async void OnAnalyticsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AnalyticsPage());
    }
}