using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PLTour.Vendor.Services;

public class MomoService : IMomoService
{
    private readonly MomoSettings _settings;
    private readonly HttpClient _httpClient;

    public MomoService(HttpClient httpClient, IOptions<MomoSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<MomoCreatePaymentResult> CreatePaymentAsync(int vendorId, string planCode, decimal amount, string orderId, string requestId)
    {
        var orderInfo = $"Nang cap {planCode} cho vendor {vendorId}";
        var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { vendorId, planCode })));
        var amountValue = ((long)amount).ToString();

        var payload = new Dictionary<string, string>
        {
            ["partnerCode"] = _settings.PartnerCode,
            ["accessKey"] = _settings.AccessKey,
            ["requestId"] = requestId,
            ["amount"] = amountValue,
            ["orderId"] = orderId,
            ["orderInfo"] = orderInfo,
            ["orderType"] = "momo_wallet",
            ["redirectUrl"] = _settings.ReturnUrl,
            ["ipnUrl"] = _settings.NotifyUrl,
            ["extraData"] = extraData,
            ["requestType"] = "captureWallet",
            ["lang"] = "vi"
        };

        var rawHash = BuildSignatureSource(requestId, orderId, amountValue, orderInfo, extraData, _settings.ReturnUrl, _settings.NotifyUrl)
            .Replace("{ACCESS_KEY}", _settings.AccessKey)
            .Replace("{PARTNER_CODE}", _settings.PartnerCode);
        payload["signature"] = ComputeHmacSha256(_settings.SecretKey, rawHash);

        using var response = await _httpClient.PostAsJsonAsync(_settings.Endpoint, payload);
        var raw = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new MomoCreatePaymentResult
            {
                Success = false,
                Message = $"HTTP {(int)response.StatusCode}",
                RawResponse = raw
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return new MomoCreatePaymentResult
            {
                Success = root.TryGetProperty("resultCode", out var rc) && rc.GetInt32() == 0,
                PayUrl = root.TryGetProperty("payUrl", out var p) ? p.GetString() : null,
                Deeplink = root.TryGetProperty("deeplink", out var d) ? d.GetString() : null,
                QrCodeUrl = root.TryGetProperty("qrCodeUrl", out var q) ? q.GetString() : null,
                Message = root.TryGetProperty("message", out var m) ? m.GetString() : null,
                RawResponse = raw
            };
        }
        catch
        {
            return new MomoCreatePaymentResult
            {
                Success = false,
                Message = "Invalid JSON from MoMo",
                RawResponse = raw
            };
        }
    }

    public bool ValidateSignature(JsonElement payload)
    {
        var signature = GetString(payload, "signature");
        if (string.IsNullOrWhiteSpace(signature)) return false;

        var raw = BuildIpnSignatureString(
            GetString(payload, "accessKey"),
            GetString(payload, "amount"),
            GetString(payload, "extraData"),
            GetString(payload, "message"),
            GetString(payload, "orderId"),
            GetString(payload, "orderInfo"),
            GetString(payload, "orderType"),
            GetString(payload, "partnerCode"),
            GetString(payload, "payType"),
            GetString(payload, "requestId"),
            GetString(payload, "responseTime"),
            GetInt(payload, "resultCode"),
            GetLong(payload, "transId"));

        var expected = ComputeHmacSha256(_settings.SecretKey, raw);
        return string.Equals(expected, signature, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSignatureSource(string requestId, string orderId, string amount, string orderInfo, string extraData, string returnUrl, string notifyUrl)
        => $"accessKey={{ACCESS_KEY}}&amount={amount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={{PARTNER_CODE}}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";

    private static string BuildIpnSignatureString(
        string accessKey, string amount, string extraData, string message, string orderId,
        string orderInfo, string orderType, string partnerCode, string payType,
        string requestId, string responseTime, int resultCode, long transId)
        => $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

    private static string ComputeHmacSha256(string secretKey, string rawData)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData))).ToLowerInvariant();
    }

    private static string GetString(JsonElement payload, string key) => payload.TryGetProperty(key, out var v) ? v.GetString() ?? string.Empty : string.Empty;
    private static int GetInt(JsonElement payload, string key) => payload.TryGetProperty(key, out var v) && v.TryGetInt32(out var i) ? i : 0;
    private static long GetLong(JsonElement payload, string key) => payload.TryGetProperty(key, out var v) && v.TryGetInt64(out var l) ? l : 0;
}
