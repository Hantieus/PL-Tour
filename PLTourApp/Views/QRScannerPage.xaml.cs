using ZXing.Net.Maui;
using PLTourApp.Database;
using PLTourApp.Models;
using PLTourApp.Engines;

namespace PLTourApp.Views;

public partial class QRScannerPage : ContentPage
{
    SQLiteHelper db;

    NarrationEngine narration;

    bool scanning = true;

    public QRScannerPage(SQLiteHelper database, NarrationEngine engine)
    {
        InitializeComponent();

        db = database;
        narration = engine;
    }

    void OnBarcodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!scanning)
            return;

        scanning = false;

        var result = e.Results.FirstOrDefault();

        if (result == null)
            return;

        string value = result.Value;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await HandleQR(value);
        });
    }

    async Task HandleQR(string code)
    {
        try
        {
            int poiId = ParsePoiId(code);

            var poi = await db.GetPoi(poiId);

            if (poi == null)
            {
                await DisplayAlert("QR", "Không tìm thấy địa điểm", "OK");
                scanning = true;
                return;
            }

            await narration.Enqueue(poi);

            await Navigation.PushAsync(
                new PoiDetailPage(poi, narration));
        }
        catch
        {
            await DisplayAlert("QR", "QR không hợp lệ", "OK");
        }

        scanning = true;
    }

    int ParsePoiId(string code)
    {
        if (code.StartsWith("poi://"))
        {
            string id = code.Replace("poi://", "");

            return int.Parse(id);
        }

        return int.Parse(code);
    }
}