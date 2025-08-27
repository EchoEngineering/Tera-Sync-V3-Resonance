using Microsoft.Extensions.Logging;
using TeraSyncV2.API.Data.Enum;
using TeraSyncV2.PlayerData.Handlers;
using TeraSyncV2.Services;
using TeraSyncV2.Services.Mediator;

namespace TeraSyncV2.PlayerData.Factories;

public class GameObjectHandlerFactory
{
    private readonly DalamudUtilService _dalamudUtilService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TeraMediator _teraMediator;
    private readonly PerformanceCollectorService _performanceCollectorService;

    public GameObjectHandlerFactory(ILoggerFactory loggerFactory, PerformanceCollectorService performanceCollectorService, TeraMediator teraMediator,
        DalamudUtilService dalamudUtilService)
    {
        _loggerFactory = loggerFactory;
        _performanceCollectorService = performanceCollectorService;
        _teraMediator = teraMediator;
        _dalamudUtilService = dalamudUtilService;
    }

    public async Task<GameObjectHandler> Create(ObjectKind objectKind, Func<nint> getAddressFunc, bool isWatched = false)
    {
        return await _dalamudUtilService.RunOnFrameworkThread(() => new GameObjectHandler(_loggerFactory.CreateLogger<GameObjectHandler>(),
            _performanceCollectorService, _teraMediator, _dalamudUtilService, objectKind, getAddressFunc, isWatched)).ConfigureAwait(false);
    }
}