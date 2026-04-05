using System.Collections.Generic;

namespace PLTour.Share.Models
{
    public class TourModel
    {
        public int Id { get; set; } // ID của Tour
        public string Name { get; set; }
        public string Duration { get; set; } // VD: "1 tiếng 45 phút"
        public string Description { get; set; }

        // Danh sách các điểm đến (POI) có trong Tour này
        public List<PoiModel> TourPois { get; set; } = new List<PoiModel>();
    }
}