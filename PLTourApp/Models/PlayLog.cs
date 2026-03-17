using SQLite;

namespace PLTourApp.Models;

public class PlayLog
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PoiId { get; set; }

    public DateTime PlayedAt { get; set; }

    public double UserLat { get; set; }

    public double UserLng { get; set; }

    public int Duration { get; set; }
}