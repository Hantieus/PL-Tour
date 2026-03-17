using PLTourApp.Models;
using PLTourApp.Engines;

namespace PLTourApp.Views;

public partial class PoiDetailPage : ContentPage
{
    Poi poi;

    NarrationEngine narration;

    public PoiDetailPage(Poi p, NarrationEngine engine)
    {
        InitializeComponent();

        poi = p;
        narration = engine;

        PoiName.Text = poi.Name;
        PoiDescription.Text = poi.Description;

        PoiImage.Source = poi.Image;
    }

    async void PlayAudio(object sender, EventArgs e)
    {
        await narration.Enqueue(poi);
    }
}