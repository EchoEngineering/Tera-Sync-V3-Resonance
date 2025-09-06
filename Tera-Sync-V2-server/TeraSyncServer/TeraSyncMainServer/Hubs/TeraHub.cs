using TeraSyncV2.API.Data;
using TeraSyncV2.API.Data.Enum;
using TeraSyncV2.API.Dto;
using TeraSyncV2.API.SignalR;
using TeraSyncV2Server.Services;
using TeraSyncV2Server.Utils;
using TeraSyncV2Shared;
using TeraSyncV2Shared.Data;
using TeraSyncV2Shared.Metrics;
using TeraSyncV2Shared.Models;
using TeraSyncV2Shared.Services;
using TeraSyncV2Shared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Collections.Concurrent;

namespace TeraSyncV2Server.Hubs;

[Authorize(Policy = "Authenticated")]
public partial class TeraHub : Hub<ITeraHub>, ITeraHub
{
    private static readonly ConcurrentDictionary<string, string> _userConnections = new(StringComparer.Ordinal);
    private readonly TeraMetrics _teraMetrics;
    private readonly SystemInfoService _systemInfoService;
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly TeraHubLogger _logger;
    private readonly string _shardName;
    private readonly int _maxExistingGroupsByUser;
    private readonly int _maxJoinedGroupsByUser;
    private readonly int _maxGroupUserCount;
    private readonly IRedisDatabase _redis;
    private readonly OnlineSyncedPairCacheService _onlineSyncedPairCacheService;
    private readonly TeraCensus _teraCensus;
    private readonly GPoseLobbyDistributionService _gPoseLobbyDistributionService;
    private readonly Uri _fileServerAddress;
    private readonly Version _expectedClientVersion;
    private readonly Lazy<TeraDbContext> _dbContextLazy;
    private TeraDbContext DbContext => _dbContextLazy.Value;
    private readonly int _maxCharaDataByUser;
    private readonly int _maxCharaDataByUserVanity;

    public TeraHub(TeraMetrics teraMetrics,
        IDbContextFactory<TeraDbContext> teraDbContextFactory, ILogger<TeraHub> logger, SystemInfoService systemInfoService,
        IConfigurationService<ServerConfiguration> configuration, IHttpContextAccessor contextAccessor,
        IRedisDatabase redisDb, OnlineSyncedPairCacheService onlineSyncedPairCacheService, TeraCensus teraCensus,
        GPoseLobbyDistributionService gPoseLobbyDistributionService)
    {
        _teraMetrics = teraMetrics;
        _systemInfoService = systemInfoService;
        _shardName = configuration.GetValue<string>(nameof(ServerConfiguration.ShardName));
        _maxExistingGroupsByUser = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxExistingGroupsByUser), 3);
        _maxJoinedGroupsByUser = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxJoinedGroupsByUser), 6);
        _maxGroupUserCount = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxGroupUserCount), 100);
        _fileServerAddress = configuration.GetValue<Uri>(nameof(ServerConfiguration.CdnFullUrl));
        _expectedClientVersion = configuration.GetValueOrDefault(nameof(ServerConfiguration.ExpectedClientVersion), new Version(0, 0, 0));
        _maxCharaDataByUser = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxCharaDataByUser), 10);
        _maxCharaDataByUserVanity = configuration.GetValueOrDefault(nameof(ServerConfiguration.MaxCharaDataByUserVanity), 50);
        _contextAccessor = contextAccessor;
        _redis = redisDb;
        _onlineSyncedPairCacheService = onlineSyncedPairCacheService;
        _teraCensus = teraCensus;
        _gPoseLobbyDistributionService = gPoseLobbyDistributionService;
        _logger = new TeraHubLogger(this, logger);
        _dbContextLazy = new Lazy<TeraDbContext>(() => teraDbContextFactory.CreateDbContext());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_dbContextLazy.IsValueCreated) DbContext.Dispose();
        }

        base.Dispose(disposing);
    }

    [Authorize(Policy = "Identified")]
    public async Task<ConnectionDto> GetConnectionDto()
    {
        _logger.LogCallInfo();

        _teraMetrics.IncCounter(MetricsAPI.CounterInitializedConnections);

        await Clients.Caller.Client_UpdateSystemInfo(_systemInfoService.SystemInfoDto).ConfigureAwait(false);

        var dbUser = await DbContext.Users.SingleAsync(f => f.UID == UserUID).ConfigureAwait(false);
        dbUser.LastLoggedIn = DateTime.UtcNow;

        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Information, "Welcome to Tera Sync V2 \"" + _shardName + "\", Current Online Users: " + _systemInfoService.SystemInfoDto.OnlineUsers).ConfigureAwait(false);

        var defaultPermissions = await DbContext.UserDefaultPreferredPermissions.SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (defaultPermissions == null)
        {
            defaultPermissions = new UserDefaultPreferredPermission()
            {
                UserUID = UserUID,
            };

            DbContext.UserDefaultPreferredPermissions.Add(defaultPermissions);
        }

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        return new ConnectionDto(new UserData(dbUser.UID, string.IsNullOrWhiteSpace(dbUser.Alias) ? null : dbUser.Alias))
        {
            CurrentClientVersion = _expectedClientVersion,
            ServerVersion = ITeraHub.ApiVersion,
            IsAdmin = dbUser.IsAdmin,
            IsModerator = dbUser.IsModerator,
            ServerInfo = new ServerInfo()
            {
                MaxGroupsCreatedByUser = _maxExistingGroupsByUser,
                ShardName = _shardName,
                MaxGroupsJoinedByUser = _maxJoinedGroupsByUser,
                MaxGroupUserCount = _maxGroupUserCount,
                FileServerAddress = _fileServerAddress,
                MaxCharaData = _maxCharaDataByUser,
                MaxCharaDataVanity = _maxCharaDataByUserVanity,
            },
            DefaultPreferredPermissions = new DefaultPermissionsDto()
            {
                DisableGroupAnimations = defaultPermissions.DisableGroupAnimations,
                DisableGroupSounds = defaultPermissions.DisableGroupSounds,
                DisableGroupVFX = defaultPermissions.DisableGroupVFX,
                DisableIndividualAnimations = defaultPermissions.DisableIndividualAnimations,
                DisableIndividualSounds = defaultPermissions.DisableIndividualSounds,
                DisableIndividualVFX = defaultPermissions.DisableIndividualVFX,
                IndividualIsSticky = defaultPermissions.IndividualIsSticky,
            },
        };
    }

    [Authorize(Policy = "Authenticated")]
    public async Task<bool> CheckClientHealth()
    {
        await UpdateUserOnRedis().ConfigureAwait(false);

        return false;
    }

    [Authorize(Policy = "Authenticated")]
    public override async Task OnConnectedAsync()
    {
        if (_userConnections.TryGetValue(UserUID, out var oldId))
        {
            _logger.LogCallWarning(TeraHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
            _userConnections[UserUID] = Context.ConnectionId;
        }
        else
        {
            _teraMetrics.IncGaugeWithLabels(MetricsAPI.GaugeConnections, labels: Continent);

            try
            {
                _logger.LogCallInfo(TeraHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                await _onlineSyncedPairCacheService.InitPlayer(UserUID).ConfigureAwait(false);
                await UpdateUserOnRedis().ConfigureAwait(false);
                _userConnections[UserUID] = Context.ConnectionId;
            }
            catch
            {
                _userConnections.Remove(UserUID, out _);
            }
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    [Authorize(Policy = "Authenticated")]
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        if (_userConnections.TryGetValue(UserUID, out var connectionId)
            && string.Equals(connectionId, Context.ConnectionId, StringComparison.Ordinal))
        {
            _teraMetrics.DecGaugeWithLabels(MetricsAPI.GaugeConnections, labels: Continent);

            try
            {
                await GposeLobbyLeave().ConfigureAwait(false);

                await _onlineSyncedPairCacheService.DisposePlayer(UserUID).ConfigureAwait(false);

                _logger.LogCallInfo(TeraHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                if (exception != null)
                    _logger.LogCallWarning(TeraHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace));

                await RemoveUserFromRedis().ConfigureAwait(false);

                _teraCensus.ClearStatistics(UserUID);

                await SendOfflineToAllPairedUsers().ConfigureAwait(false);

                DbContext.RemoveRange(DbContext.Files.Where(f => !f.Uploaded && f.UploaderUID == UserUID));
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

            }
            catch { }
            finally
            {
                _userConnections.Remove(UserUID, out _);
            }
        }
        else
        {
            _logger.LogCallWarning(TeraHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
