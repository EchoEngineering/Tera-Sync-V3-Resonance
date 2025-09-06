using TeraSyncV2.API.Dto;
using TeraSyncV2.API.SignalR;
using TeraSyncV2Server.Hubs;
using TeraSyncV2Shared.Data;
using TeraSyncV2Shared.Metrics;
using TeraSyncV2Shared.Services;
using TeraSyncV2Shared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace TeraSyncV2Server.Services;

public sealed class SystemInfoService : BackgroundService
{
    private readonly TeraMetrics _teraMetrics;
    private readonly IConfigurationService<ServerConfiguration> _config;
    private readonly IDbContextFactory<TeraDbContext> _dbContextFactory;
    private readonly ILogger<SystemInfoService> _logger;
    private readonly IHubContext<TeraHub, ITeraHub> _hubContext;
    private readonly IRedisDatabase _redis;
    public SystemInfoDto SystemInfoDto { get; private set; } = new();

    public SystemInfoService(TeraMetrics teraMetrics, IConfigurationService<ServerConfiguration> configurationService, IDbContextFactory<TeraDbContext> dbContextFactory,
        ILogger<SystemInfoService> logger, IHubContext<TeraHub, ITeraHub> hubContext, IRedisDatabase redisDb)
    {
        _teraMetrics = teraMetrics;
        _config = configurationService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _hubContext = hubContext;
        _redis = redisDb;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("System Info Service started");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var timeOut = _config.IsMain ? 15 : 30;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);

                _teraMetrics.SetGaugeTo(MetricsAPI.GaugeAvailableWorkerThreads, workerThreads);
                _teraMetrics.SetGaugeTo(MetricsAPI.GaugeAvailableIOWorkerThreads, ioThreads);

                var onlineUsers = (_redis.SearchKeysAsync("UID:*").GetAwaiter().GetResult()).Count();
                SystemInfoDto = new SystemInfoDto()
                {
                    OnlineUsers = onlineUsers,
                };

                if (_config.IsMain)
                {
                    _logger.LogInformation("Sending System Info, Online Users: {onlineUsers}", onlineUsers);

                    await _hubContext.Clients.All.Client_UpdateSystemInfo(SystemInfoDto).ConfigureAwait(false);

                    using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

                    _teraMetrics.SetGaugeTo(MetricsAPI.GaugeAuthorizedConnections, onlineUsers);
                    _teraMetrics.SetGaugeTo(MetricsAPI.GaugePairs, db.ClientPairs.AsNoTracking().Count());
                    _teraMetrics.SetGaugeTo(MetricsAPI.GaugePairsPaused, db.Permissions.AsNoTracking().Where(p => p.IsPaused).Count());
                    _teraMetrics.SetGaugeTo(MetricsAPI.GaugeGroups, db.Groups.AsNoTracking().Count());
                    _teraMetrics.SetGaugeTo(MetricsAPI.GaugeGroupPairs, db.GroupPairs.AsNoTracking().Count());
                    _teraMetrics.SetGaugeTo(MetricsAPI.GaugeUsersRegistered, db.Users.AsNoTracking().Count());
                }

                await Task.Delay(TimeSpan.FromSeconds(timeOut), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push system info");
            }
        }
    }
}