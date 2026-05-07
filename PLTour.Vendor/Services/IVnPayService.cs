namespace PLTour.Vendor.Services;

public interface IVnPayService
{
    string CreatePaymentUrl(int vendorId, string planCode, decimal amount, string orderId);
    bool ValidateSignature(IQueryCollection query);
}
