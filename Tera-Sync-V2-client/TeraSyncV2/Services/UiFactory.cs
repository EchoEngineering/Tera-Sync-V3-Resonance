using Microsoft.Extensions.Logging;
using TeraSyncV2.API.Dto.Group;
using TeraSyncV2.PlayerData.Pairs;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.Services.ServerConfiguration;
using TeraSyncV2.UI;
using TeraSyncV2.UI.Components.Popup;
using TeraSyncV2.WebAPI;

namespace TeraSyncV2.Services;

public class UiFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly TeraMediator _teraMediator;
    private readonly ApiController _apiController;
    private readonly UiSharedService _uiSharedService;
    private readonly PairManager _pairManager;
    private readonly ServerConfigurationManager _serverConfigManager;
    private readonly TeraProfileManager _teraProfileManager;
    private readonly PerformanceCollectorService _performanceCollectorService;

    public UiFactory(ILoggerFactory loggerFactory, TeraMediator teraMediator, ApiController apiController,
        UiSharedService uiSharedService, PairManager pairManager, ServerConfigurationManager serverConfigManager,
        TeraProfileManager teraProfileManager, PerformanceCollectorService performanceCollectorService)
    {
        _loggerFactory = loggerFactory;
        _teraMediator = teraMediator;
        _apiController = apiController;
        _uiSharedService = uiSharedService;
        _pairManager = pairManager;
        _serverConfigManager = serverConfigManager;
        _teraProfileManager = teraProfileManager;
        _performanceCollectorService = performanceCollectorService;
    }

    public SyncshellAdminUI CreateSyncshellAdminUi(GroupFullInfoDto dto)
    {
        return new SyncshellAdminUI(_loggerFactory.CreateLogger<SyncshellAdminUI>(), _teraMediator,
            _apiController, _uiSharedService, _pairManager, dto, _performanceCollectorService);
    }

    public StandaloneProfileUi CreateStandaloneProfileUi(Pair pair)
    {
        return new StandaloneProfileUi(_loggerFactory.CreateLogger<StandaloneProfileUi>(), _teraMediator,
            _uiSharedService, _serverConfigManager, _teraProfileManager, _pairManager, pair, _performanceCollectorService);
    }

    public PermissionWindowUI CreatePermissionPopupUi(Pair pair)
    {
        return new PermissionWindowUI(_loggerFactory.CreateLogger<PermissionWindowUI>(), pair,
            _teraMediator, _uiSharedService, _apiController, _performanceCollectorService);
    }
}
