using Microsoft.Maui;
using Microsoft.Maui.Controls;
using PLTour.App.Pages;
using PLTour.App.Services;

namespace PLTour.App;

public partial class App : Application
{
    private readonly LoadingPage _loadingPage;

    public App(LocationService locationService)
    {
        InitializeComponent();
        _loadingPage = new LoadingPage(locationService);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_loadingPage);
    }
}
