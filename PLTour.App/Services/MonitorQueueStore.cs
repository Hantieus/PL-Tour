using System.Text.Json;

namespace PLTour.App.Services;

public sealed class MonitorQueueStore
{
    private const string StorageFileName = "monitor-queue.json";

    public async Task SaveAsync(IEnumerable<MonitorQueueItem> items)
    {
        var path = GetPath();
        var json = JsonSerializer.Serialize(items);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<List<MonitorQueueItem>> LoadAsync()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return new List<MonitorQueueItem>();

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<MonitorQueueItem>>(json) ?? new List<MonitorQueueItem>();
        }
        catch
        {
            return new List<MonitorQueueItem>();
        }
    }

    public Task ClearAsync()
    {
        try
        {
            var path = GetPath();
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }

        return Task.CompletedTask;
    }

    private static string GetPath() => Path.Combine(FileSystem.Current.AppDataDirectory, StorageFileName);
}

public sealed record MonitorQueueItem(string Url, string PayloadJson, string Label, int Attempt = 0);
