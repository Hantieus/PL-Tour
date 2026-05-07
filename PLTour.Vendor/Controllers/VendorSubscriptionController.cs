using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Vendor.Services;
using PLTour.Vendor.ViewModels;
using VendorEntity = PLTour.Shared.Models.Entities.Vendor;
using VendorSubscriptionTransactionEntity = PLTour.Shared.Models.Entities.VendorSubscriptionTransaction;

namespace PLTour.Vendor.Controllers;

[Authorize(AuthenticationSchemes = "VendorAuth")]
public class VendorSubscriptionController : Controller
{
    private readonly PLTourDbContext _context;
    private readonly IMomoService _momoService;

    public VendorSubscriptionController(PLTourDbContext context, IMomoService momoService)
    {
        _context = context;
        _momoService = momoService;
    }

    private int GetVendorId() => int.TryParse(User.FindFirst("VendorId")?.Value, out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vendor = await GetVendorAsync();
        if (vendor is null) return Redirect("/vendor-login/login");

        ViewBag.PremiumPrice = 199000m;
        ViewBag.StoreCount = await _context.Set<VendorStore>().CountAsync(x => x.VendorId == vendor.VendorId);
        return View(vendor);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePayment(VendorSubscriptionViewModel model)
    {
        var vendor = await GetVendorAsync();
        if (vendor is null) return Redirect("/vendor-login/login");

        if (model.PlanCode != "Premium")
        {
            ModelState.AddModelError(nameof(model.PlanCode), "Gói không hợp lệ");
            ViewBag.PremiumPrice = 199000m;
            return View("Index", vendor);
        }

        if (model.Amount <= 0)
        {
            model.Amount = 199000m;
        }

        var orderId = $"MOMO{vendor.VendorId}{DateTime.UtcNow:yyyyMMddHHmmss}";
        var requestId = $"REQ{DateTime.UtcNow:yyyyMMddHHmmss}";
        var transaction = new VendorSubscriptionTransactionEntity
        {
            VendorId = vendor.VendorId,
            PlanCode = model.PlanCode,
            Amount = model.Amount,
            Currency = "VND",
            Status = "Pending",
            PaymentMethod = "MoMo",
            OrderId = orderId
        };

        _context.Set<VendorSubscriptionTransactionEntity>().Add(transaction);
        await _context.SaveChangesAsync();

        var result = await _momoService.CreatePaymentAsync(vendor.VendorId, model.PlanCode, model.Amount, orderId, requestId);
        transaction.RawResponse = result.RawResponse;
        transaction.Status = result.Success ? "AwaitingPayment" : "Failed";
        await _context.SaveChangesAsync();

        if (!result.Success || string.IsNullOrWhiteSpace(result.PayUrl))
        {
            TempData["ErrorMessage"] = $"Không tạo được link thanh toán MoMo: {result.Message ?? "unknown"}";
            return RedirectToAction(nameof(Index));
        }

        return Redirect(result.PayUrl);
    }

    [HttpGet]
    public async Task<IActionResult> MomoReturn([FromQuery] string orderId, [FromQuery] string resultCode)
        => await HandlePaymentCallback(orderId, resultCode);

    [HttpPost]
    public async Task<IActionResult> MomoNotify([FromBody] JsonElement payload)
    {
        var orderId = GetString(payload, "orderId");
        var resultCode = GetString(payload, "resultCode");

        var transaction = await _context.Set<VendorSubscriptionTransactionEntity>().FirstOrDefaultAsync(x => x.OrderId == orderId);
        if (transaction is null) return Ok(new { resultCode = 0, message = "ignored" });

        transaction.RawResponse = payload.GetRawText();
        if (!_momoService.ValidateSignature(payload))
        {
            transaction.Status = "Failed";
            await _context.SaveChangesAsync();
            return Ok(new { resultCode = 0, message = "signature invalid" });
        }

        if (resultCode == "0")
        {
            transaction.Status = "Paid";
            transaction.PaidAt = DateTime.UtcNow;
            transaction.ExpiresAt = DateTime.UtcNow.AddMonths(1);

            var vendor = await _context.Set<VendorEntity>().FindAsync(transaction.VendorId);
            if (vendor is not null)
            {
                vendor.Plan = "Premium";
                vendor.PlanExpiresAt = transaction.ExpiresAt;
                vendor.UpdatedDate = DateTime.UtcNow;
            }
        }
        else
        {
            transaction.Status = "Failed";
        }

        await _context.SaveChangesAsync();
        return Ok(new { resultCode = 0, message = "processed" });
    }

    private async Task<IActionResult> HandlePaymentCallback(string orderId, string resultCode)
    {
        var transaction = await _context.Set<VendorSubscriptionTransactionEntity>()
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (transaction is null) return RedirectToAction(nameof(Index));

        transaction.RawResponse = JsonSerializer.Serialize(Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()));

        if (resultCode == "0")
        {
            transaction.Status = "Paid";
            transaction.PaidAt = DateTime.UtcNow;
            transaction.ExpiresAt = DateTime.UtcNow.AddMonths(1);

            var vendor = await _context.Set<VendorEntity>().FindAsync(transaction.VendorId);
            if (vendor is not null)
            {
                vendor.Plan = "Premium";
                vendor.PlanExpiresAt = transaction.ExpiresAt;
                vendor.UpdatedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Success));
        }

        transaction.Status = "Failed";
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Failed));
    }

    private static string GetString(JsonElement payload, string key) => payload.TryGetProperty(key, out var v) ? v.GetString() ?? string.Empty : string.Empty;

    [HttpGet]
    public IActionResult Success() => View();

    [HttpGet]
    public IActionResult Failed() => View();

    private async Task<VendorEntity?> GetVendorAsync()
    {
        var vendorId = GetVendorId();
        if (vendorId == 0) return null;
        return await _context.Set<VendorEntity>().FirstOrDefaultAsync(x => x.VendorId == vendorId);
    }
}
