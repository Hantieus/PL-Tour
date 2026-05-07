using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PLTour.API.Services;

public class ActiveDeviceCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public ActiveDeviceCleanupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await MarkOfflineAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // swallow to keep background service alive
            }
        }
    }

    private async Task MarkOfflineAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PLTourDbContext>();
        var cutoff = DateTime.UtcNow.AddMinutes(-10);

        var staleDevices = await context.ActiveDevices
            .Where(x => x.LastHeartbeat < cutoff && x.Status != "offline")
            .ToListAsync(stoppingToken);

        foreach (var device in staleDevices)
        {
            device.Status = "offline";
        }

        if (staleDevices.Count > 0)
            await context.SaveChangesAsync(stoppingToken);
    }
}
