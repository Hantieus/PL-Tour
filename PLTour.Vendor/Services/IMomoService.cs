using System.Text.Json;

namespace PLTour.Vendor.Services;

public interface IMomoService
{
    Task<MomoCreatePaymentResult> CreatePaymentAsync(int vendorId, string planCode, decimal amount, string orderId, string requestId);
    bool ValidateSignature(JsonElement payload);
}
