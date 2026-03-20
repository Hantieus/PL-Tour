using SQLite;

namespace PLTourApp.Models;

public class UserRoute
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public DateTime TimeStamp { get; set; }
}