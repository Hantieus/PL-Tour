using Microsoft.Maui.Controls;
using PLTour.App.Models;

namespace PLTour.App.Controls;

public partial class PoiDetailPopup : ContentView
{
    public static readonly BindableProperty PoiDataProperty = BindableProperty.Create(
        nameof(PoiData), typeof(PoiModel), typeof(PoiDetailPopup), null, propertyChanged: OnPoiDataChanged);

    public static readonly BindableProperty IsPopupVisibleProperty = BindableProperty.Create(
        nameof(IsPopupVisible), typeof(bool), typeof(PoiDetailPopup), false, BindingMode.TwoWay,
        propertyChanged: OnIsPopupVisibleChanged);

    public PoiModel? PoiData
    {
        get => (PoiModel?)GetValue(PoiDataProperty);
        set => SetValue(PoiDataProperty, value);
    }

    public bool IsPopupVisible
    {
        get => (bool)GetValue(IsPopupVisibleProperty);
        set => SetValue(IsPopupVisibleProperty, value);
    }

    public event EventHandler? CloseRequested;
    public event EventHandler<PoiModel?>? SpeakRequested;
    public event EventHandler<PoiModel?>? ViewMapRequested;
    // Backward-compatible aliases used by existing XAML/code-behind
    public event EventHandler<PoiModel?>? SpeakButtonClicked;
    public event EventHandler<PoiModel?>? ViewMapButtonClicked;

    public PoiDetailPopup()
    {
        InitializeComponent();
    }

    static void OnPoiDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PoiDetailPopup popup)
            popup.BindingContext = newValue;
    }

    static void OnIsPopupVisibleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PoiDetailPopup popup)
            popup.IsVisible = (bool)newValue;
    }

    public void ShowPopup(PoiModel poi)
    {
        PoiData = poi;
        IsPopupVisible = true;
    }

    public void HidePopup()
    {
        IsPopupVisible = false;
        PoiData = null;
    }

    private void ClosePopup_Clicked(object sender, EventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        HidePopup();
    }

    private void BtnSpeak_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (PoiData == null) return;

            SpeakRequested?.Invoke(this, PoiData);
            SpeakButtonClicked?.Invoke(this, PoiData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POI POPUP] Speak failed: {ex}");
        }
    }

    private void BtnViewMap_Clicked(object sender, EventArgs e)
    {
        ViewMapRequested?.Invoke(this, PoiData);
        ViewMapButtonClicked?.Invoke(this, PoiData);
    }
}
