using PLTourApp.Models;

namespace PLTourApp.Database;

public static class SeedData
{
    public static async Task Seed(SQLiteHelper db)
    {
        var pois = await db.GetPois();

        if (pois.Count > 0)
            return;

        await db.InsertPoi(new Poi
        {
            Name = "Bến Thành Market",
            Description = "Chợ Bến Thành nổi tiếng",
            Latitude = 10.772,
            Longitude = 106.698,
            Radius = 80,
            Priority = 1,
            TtsScript = "Bạn đang đến Chợ Bến Thành",
            AudioFile = "benthanh.mp3"
        });

        await db.InsertPoi(new Poi
        {
            Name = "Nhà thờ Đức Bà",
            Description = "Nhà thờ cổ nổi tiếng",
            Latitude = 10.779,
            Longitude = 106.699,
            Radius = 100,
            Priority = 1,
            TtsScript = "Đây là Nhà thờ Đức Bà"
        });
    }
}