using Microsoft.Extensions.DependencyInjection;
using PLTour.App.Models;
using PLTour.App.Services;
using ZXing.Net.Maui;

namespace PLTour.App.Pages;

public partial class QrScannerPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly IAudioService _audioService;
    private readonly DeduplicationService _deduplicationService;
    private readonly QueuedActionService _queueService;
    private PoiModel? _currentPoi;
    private bool _isHandlingScan;
    private string? _lastScanValue;
    private DateTime _lastScanAt = DateTime.MinValue;

    public QrScannerPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _apiService = services?.GetService<ApiService>() ?? new ApiService();
        _audioService = services?.GetService<IAudioService>() ?? new AudioService();
        _deduplicationService = services?.GetService<DeduplicationService>() ?? new DeduplicationService();
        _queueService = services?.GetService<QueuedActionService>() ?? new QueuedActionService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        QrCameraView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = false,
            Formats = BarcodeFormats.TwoDimensional
        };

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Chưa có quyền camera.";
            await DisplayAlertAsync("Thông báo", "Bạn cần cấp quyền camera để quét QR.", "OK");
            QrCameraView.IsDetecting = false;
            return;
        }

        QrCameraView.IsDetecting = true;
        StatusLabel.Text = "Sẵn sàng quét...";
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        QrCameraView.IsDetecting = false;
        await _audioService.StopAsync();
        SetPlayingState(false);
    }

    private async void QrCameraView_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isHandlingScan) return;

        var result = e.Results?.FirstOrDefault()?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(result)) return;

        var dedupeKey = _deduplicationService.BuildKey("qr_scan", result.ToLowerInvariant());
        if (!_deduplicationService.ShouldProcess(dedupeKey, TimeSpan.FromSeconds(5)))
            return;

        if (string.Equals(_lastScanValue, result, StringComparison.OrdinalIgnoreCase) &&
            DateTime.UtcNow - _lastScanAt < TimeSpan.FromSeconds(5))
            return;

        _lastScanValue = result;
        _lastScanAt = DateTime.UtcNow;

        _isHandlingScan = true;
        QrCameraView.IsDetecting = false;

        await _queueService.EnqueueAsync(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                StatusLabel.Text = $"Đã quét: {result}";
                var poi = await ResolvePoiFromQrAsync(result);
                if (poi == null)
                {
                    await DisplayAlertAsync("QR không hợp lệ", "Không tìm thấy địa điểm từ mã QR này.", "OK");
                    ResetScanner();
                    return;
                }

                _currentPoi = poi;
                PoiDetailPopupView.ShowPopup(poi);
                await SpeakPoiAsync(poi);
            });
        });
    }

    private async Task<PoiModel?> ResolvePoiFromQrAsync(string rawValue)
    {
        var allPois = await _apiService.GetAllLocationsAsync();
        if (allPois.Count == 0) return null;

        var locationId = ParseLocationId(rawValue);
        if (locationId.HasValue)
        {
            return allPois.FirstOrDefault(p => p.Id == locationId.Value);
        }

        return allPois.FirstOrDefault(p =>
            string.Equals(p.Name, rawValue, StringComparison.OrdinalIgnoreCase));
    }

    private static int? ParseLocationId(string rawValue)
    {
        if (int.TryParse(rawValue, out var directId))
            return directId;

        if (Uri.TryCreate(rawValue, UriKind.Absolute, out var uri))
        {
            var segments = uri.Segments;
            var last = segments.LastOrDefault()?.Trim('/');
            if (int.TryParse(last, out var idFromUrl))
                return idFromUrl;
        }

        return null;
    }

    private async Task SpeakPoiAsync(PoiModel poi)
    {
        try
        {
            if (_currentPoi?.Id == poi.Id && _audioService.IsPlaying)
                return;

            await _audioService.StopAsync();
            SetPlayingState(false);

            poi.IsPlaying = true;

            if (!string.IsNullOrWhiteSpace(poi.AudioUrl))
            {
                await _queueService.EnqueueAsync(() => _audioService.PlayAudioAsync(poi.AudioUrl));
            }
            else
            {
                var content = string.IsNullOrWhiteSpace(poi.FullContent) ? poi.Description : poi.FullContent;
                if (string.IsNullOrWhiteSpace(content))
                    content = "Không có thông tin thuyết minh.";

                var langCode = Preferences.Default.Get("UserLanguage", poi.LanguageCode ?? "vi");
                await _queueService.EnqueueAsync(() => _audioService.PlayTextToSpeechAsync($"{poi.Name}. {content}", langCode));
            }
        }
        catch (Exception ex)
        {
            poi.IsPlaying = false;
            await DisplayAlertAsync("Lỗi âm thanh", $"Không thể phát thuyết minh: {ex.Message}", "OK");
        }
    }

    private async void PoiDetailPopupView_SpeakRequested(object sender, PoiModel? poi)
    {
        if (poi == null) return;
        await SpeakPoiAsync(poi);
    }

    private async void PoiDetailPopupView_CloseRequested(object sender, EventArgs e)
    {
        PoiDetailPopupView.HidePopup();
        await _audioService.StopAsync();
        SetPlayingState(false);
        _queueService.Clear();
        ResetScanner();
    }

    private async void PoiDetailPopupView_ViewMapRequested(object sender, PoiModel? poi)
    {
        if (poi == null) return;

        await _audioService.StopAsync();
        SetPlayingState(false);
        PoiDetailPopupView.HidePopup();

        var keyword = Uri.EscapeDataString(poi.Name ?? string.Empty);
        await Shell.Current.GoToAsync($"//map?ScannedPoiName={keyword}");
    }

    private void ScanAgain_Clicked(object sender, EventArgs e)
    {
        _ = _audioService.StopAsync();
        SetPlayingState(false);
        PoiDetailPopupView.HidePopup();
        ResetScanner();
    }

    private void ResetScanner()
    {
        _isHandlingScan = false;
        _currentPoi = null;
        StatusLabel.Text = "Sẵn sàng quét...";
        QrCameraView.IsDetecting = true;
    }

    private void SetPlayingState(bool value)
    {
        if (_currentPoi != null)
        {
            _currentPoi.IsPlaying = value;
        }
    }
}