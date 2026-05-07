using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using PLTour.App.Services;

namespace PLTour.App.Converters;

[ContentProperty(nameof(Key))]
public class LocalizationExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Source = LocalizationService.Instance,
            Path = $"[{Key}]",
            Mode = BindingMode.OneWay
        };
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
        => ProvideValue(serviceProvider);
}
