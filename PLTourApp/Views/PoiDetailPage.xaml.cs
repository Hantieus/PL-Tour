using PLTourApp.Models;
using PLTourApp.Engines;

namespace PLTourApp.Views;

public partial class PoiDetailPage : ContentPage
{
    private Poi poi;
    private NarrationEngine narration;

    public PoiDetailPage(Poi p, NarrationEngine engine)
    {
        InitializeComponent();

        poi = p;
        narration = engine;

        LoadData();
    }

    private void LoadData()
    {
        PoiName.Text = poi.Name;
        PoiDescription.Text = poi.Description;
        PoiImage.Source = poi.Image;
    }

    private async void PlayAudio(object sender, EventArgs e)
    {
        try
        {
            await narration.Enqueue(poi);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", ex.Message, "OK");
        }
    }
}