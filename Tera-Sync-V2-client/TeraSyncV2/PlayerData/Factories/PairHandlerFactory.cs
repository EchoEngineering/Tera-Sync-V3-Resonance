using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeraSyncV2.FileCache;
using TeraSyncV2.Interop.Ipc;
using TeraSyncV2.PlayerData.Handlers;
using TeraSyncV2.PlayerData.Pairs;
using TeraSyncV2.Services;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.Services.ServerConfiguration;

namespace TeraSyncV2.PlayerData.Factories;

public class PairHandlerFactory
{
    private readonly DalamudUtilService _dalamudUtilService;
    private readonly FileCacheManager _fileCacheManager;
    private readonly FileDownloadManagerFactory _fileDownloadManagerFactory;
    private readonly GameObjectHandlerFactory _gameObjectHandlerFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IpcManager _ipcManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TeraMediator _teraMediator;
    private readonly PlayerPerformanceService _playerPerformanceService;
    private readonly ServerConfigurationManager _serverConfigManager;
    private readonly PluginWarningNotificationService _pluginWarningNotificationManager;

    public PairHandlerFactory(ILoggerFactory loggerFactory, GameObjectHandlerFactory gameObjectHandlerFactory, IpcManager ipcManager,
        FileDownloadManagerFactory fileDownloadManagerFactory, DalamudUtilService dalamudUtilService,
        PluginWarningNotificationService pluginWarningNotificationManager, IHostApplicationLifetime hostApplicationLifetime,
        FileCacheManager fileCacheManager, TeraMediator teraMediator, PlayerPerformanceService playerPerformanceService,
        ServerConfigurationManager serverConfigManager)
    {
        _loggerFactory = loggerFactory;
        _gameObjectHandlerFactory = gameObjectHandlerFactory;
        _ipcManager = ipcManager;
        _fileDownloadManagerFactory = fileDownloadManagerFactory;
        _dalamudUtilService = dalamudUtilService;
        _pluginWarningNotificationManager = pluginWarningNotificationManager;
        _hostApplicationLifetime = hostApplicationLifetime;
        _fileCacheManager = fileCacheManager;
        _teraMediator = teraMediator;
        _playerPerformanceService = playerPerformanceService;
        _serverConfigManager = serverConfigManager;
    }

    public PairHandler Create(Pair pair)
    {
        return new PairHandler(_loggerFactory.CreateLogger<PairHandler>(), pair, _gameObjectHandlerFactory,
            _ipcManager, _fileDownloadManagerFactory.Create(), _pluginWarningNotificationManager, _dalamudUtilService, _hostApplicationLifetime,
            _fileCacheManager, _teraMediator, _playerPerformanceService, _serverConfigManager);
    }
}