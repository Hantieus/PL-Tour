using Microsoft.Maui;
using Microsoft.Maui.Controls;
using PLTour.App.Pages;
using PLTour.App.Services;

namespace PLTour.App;

public partial class App : Application
{
    private readonly LoadingPage _loadingPage;
    private readonly DeviceMonitorService _deviceMonitorService;

    public App(LocationService locationService, DeviceMonitorService deviceMonitorService)
    {
        InitializeComponent();
        _loadingPage = new LoadingPage(locationService);
        _deviceMonitorService = deviceMonitorService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_loadingPage);
        window.Created += (_, __) => MainThread.BeginInvokeOnMainThread(() => _deviceMonitorService.Start());
        return window;
    }
}
