namespace PLTour.Shared.Models.DTO;

public class TourDto
{
    public int TourId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string IntroText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    // Chứa danh sách các địa điểm đã được sắp xếp theo OrderIndex
    public List<LocationDto> Locations { get; set; } = new();
}