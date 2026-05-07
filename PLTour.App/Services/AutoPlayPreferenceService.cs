using Microsoft.Maui.Storage;

namespace PLTour.App.Services;

public sealed class AutoPlayPreferenceService
{
    public static AutoPlayPreferenceService Instance { get; } = new();

    private const string EnabledKey = "PLTour.AutoPlayNearPoiEnabled";

    public bool IsEnabled => Preferences.Default.Get(EnabledKey, true);

    public bool SetEnabled(bool enabled)
    {
        Preferences.Default.Set(EnabledKey, enabled);
        return enabled;
    }
}
