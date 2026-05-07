namespace PLTour.Vendor.Services;

public sealed class MomoCreatePaymentResult
{
    public bool Success { get; init; }
    public string? PayUrl { get; init; }
    public string? Deeplink { get; init; }
    public string? QrCodeUrl { get; init; }
    public string? Message { get; init; }
    public string? RawResponse { get; init; }
}
