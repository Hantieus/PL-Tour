using SQLite;

namespace PLTourApp.Models;

public class Audio
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int PoiId { get; set; }

    public string Language { get; set; }

    public string FilePath { get; set; }

    public int Duration { get; set; }
}