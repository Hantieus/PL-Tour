using SQLite;

namespace PLTourApp.Models;

public class Poi
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int TourId { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Radius { get; set; }

    public int Priority { get; set; }

    public string AudioFile { get; set; }

    public string TtsScript { get; set; }

    public string Image { get; set; }

    public string MapLink { get; set; }

    public string Language { get; set; }

}