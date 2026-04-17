namespace PLTour.Shared.Models.DTO
{
    public class VendorDto
    {
        public int VendorId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public List<ProductDto>? Products { get; set; }
    }
}