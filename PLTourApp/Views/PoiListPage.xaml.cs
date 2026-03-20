using PLTourApp.ViewModels;
using System.Collections;

namespace PLTourApp.Views;

public partial class PoiListPage : ContentPage
{
    MapViewModel viewModel;

    public PoiListPage(MapViewModel vm)
    {
        InitializeComponent();

        viewModel = vm;

        PoiList.ItemsSource = viewModel.Pois;
    }
}