namespace PLTour.Shared.Models.DTO;

public class TourNarrationDto
{
    public int TourNarrationId { get; set; }
    public int LanguageId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsDefault { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
}
