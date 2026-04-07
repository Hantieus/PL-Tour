using SQLite;

namespace PLTour.App.Models;

public class PoiHistory
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Time { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
}