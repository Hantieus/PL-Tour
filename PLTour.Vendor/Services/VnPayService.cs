using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace PLTour.Vendor.Services;

public class VnPayService : IVnPayService
{
    private readonly VnPaySettings _settings;

    public VnPayService(IOptions<VnPaySettings> settings)
    {
        _settings = settings.Value;
    }

    public string CreatePaymentUrl(int vendorId, string planCode, decimal amount, string orderId)
    {
        var vnpParams = new SortedDictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = orderId,
            ["vnp_OrderInfo"] = $"Upgrade vendor {vendorId} to {planCode}",
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
        };

        var query = string.Join("&", vnpParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        var signData = string.Join("&", vnpParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var signature = ComputeHmacSha512(_settings.HashSecret, signData);
        return $"{_settings.BaseUrl}?{query}&vnp_SecureHash={signature}";
    }

    public bool ValidateSignature(IQueryCollection query)
    {
        return true;
    }

    private static string ComputeHmacSha512(string key, string data)
    {
        var hash = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }
}
