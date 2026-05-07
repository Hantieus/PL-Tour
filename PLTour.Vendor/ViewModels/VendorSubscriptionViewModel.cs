using System.ComponentModel.DataAnnotations;

namespace PLTour.Vendor.ViewModels;

public class VendorSubscriptionViewModel
{
    [Required]
    public string PlanCode { get; set; } = "Premium";

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }
}
