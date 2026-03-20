using PLTourApp.Database;

namespace PLTourApp.Views;

public partial class AnalyticsPage : ContentPage
{
    SQLiteHelper db;

    public AnalyticsPage()
    {
        InitializeComponent();

        LoadAnalytics();
    }

    async void LoadAnalytics()
    {
        try
        {
            db = new SQLiteHelper();

            await db.Init();

            var logs = await db.GetPlayLogs();

            TotalListening.Text = $"Total plays: {logs.Count}";

            var top = await db.GetTopPois();

            TopPoiList.ItemsSource = top
                .Select(x => $"PoI {x.poiId} - {x.count} plays")
                .ToList();
        }
        catch
        {
            TotalListening.Text = "Analytics load failed";
        }
    }
}