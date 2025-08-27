using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeraSyncV2.FileCache;
using TeraSyncV2.TeraSyncConfiguration;
using TeraSyncV2.PlayerData.Pairs;
using TeraSyncV2.PlayerData.Services;
using TeraSyncV2.Services;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.Services.ServerConfiguration;
using System.Reflection;

namespace TeraSyncV2;

#pragma warning disable S125 // Sections of code should not be commented out
/*
                                                                    (..,,...,,,,,+/,                ,,.....,,+
                                                              ..,,+++/((###%%%&&%%#(+,,.,,,+++,,,,//,,#&@@@@%+.
                                                          ...+//////////(/,,,,++,.,(###((//////////,..  .,#@@%/./
                                                       ,..+/////////+///,.,. ,&@@@@,,/////////////+,..    ,(##+,.
                                                    ,,.+//////////++++++..     ./#%#,+/////////////+,....,/((,..,
                                                  +..////////////+++++++...  .../##(,,////////////////++,,,+/(((+,
                                                +,.+//////////////+++++++,.,,,/(((+.,////////////////////////((((#/,,
                                              /+.+//////////++++/++++++++++,,...,++///////////////////////////((((##,
                                             /,.////////+++++++++++++++++++++////////+++//////++/+++++//////////((((#(+,
                                           /+.+////////+++++++++++++++++++++++++++++++++++++++++++++++++++++/////((((##+
                                          +,.///////////////+++++++++++++++++++++++++++++++++++++++++++++++++++///((((%/
                                         /.,/////////////////+++++++++++++++++++++++++++++++++++++++++++++++++++///+/(#+
                                        +,./////////////////+++++++++++++++++++++++++++++++++++++++++++++++,,+++++///((,
                                       ...////////++/++++++++++++++++++++++++,,++++++++++++++++++++++++++++++++++++//(,,
                                       ..//+,+///++++++++++++++++++,,,,+++,,,,,,,,,,,,++++++++,,+++++++++++++++++++//,,+
                                      ..,++,.++++++++++++++++++++++,,,,,,,,,,,,,,,,,,,++++++++,,,,,,,,,,++++++++++...
                                      ..+++,.+++++++++++++++++++,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,++,..,.
                                     ..,++++,,+++++++++++,+,,,,,,,,,,..,+++++++++,,,,,,.....................,//+,+
                                 ....,+++++,.,+++++++++++,,,,,,,,.+///(((((((((((((///////////////////////(((+,,,
                          .....,++++++++++..,+++++++++++,,.,,,.////////(((((((((((((((////////////////////+,,/
                      .....,++++++++++++,..,,+++++++++,,.,../////////////////((((((((((//////////////////,,+
                   ...,,+++++++++++++,.,,.,,,+++++++++,.,/////////////////(((//++++++++++++++//+++++++++/,,
                ....,++++++++++++++,.,++.,++++++++++++.,+////////////////////+++++++++++++++++++++++++///,,..
              ...,++++++++++++++++..+++..+++++++++++++.,//////////////////////////++++++++++++///////++++......
            ...++++++++++++++++++..++++.,++,++++++++++.+///////////////////////////////////////////++++++..,,,..
          ...+++++++++++++++++++..+++++..,+,,+++++++++.+//////////////////////////////////////////+++++++...,,,,..
         ..++++++++++++++++++++..++++++..,+,,+++++++++.+//////////////////////////////////////++++++++++,....,,,,..
       ...+++//(//////+++++++++..++++++,.,+++++++++++++,..,....,,,+++///////////////////////++++++++++++..,,,,,,,,...
      ..,++/(((((//////+++++++,.,++++++,,.,,,+++++++++++++++++++++++,.++////////////////////+++++++++++.....,,,,,,,...
     ..,//#(((((///////+++++++..++++++++++,...,++,++++++++++++++++,...+++/////////////////////+,,,+++...  ....,,,,,,...
   ...+//(((((//////////++++++..+++++++++++++++,......,,,,++++++,,,..+++////////////////////////+,....     ...,,,,,,,...
   ..,//((((////////////++++++..++++++/+++++++++++++,,...,,........,+/+//////////////////////((((/+,..     ....,.,,,,..
  ...+/////////////////////+++..++++++/+///+++++++++++++++++++++///+/+////////////////////////(((((/+...   .......,,...
  ..++////+++//////////////++++.+++++++++///////++++++++////////////////////////////////////+++/(((((/+..    .....,,...
  .,++++++++///////////////++++..++++//////////////////////////////////////////////////////++++++/((((++..    ........
  .+++++++++////////////////++++,.+++/////////////////////////////////////////////////////+++++++++/((/++..
 .,++++++++//////////////////++++,.+++//////////////////////////////////////////////////+++++++++++++//+++..
 .++++++++//////////////////////+/,.,+++////((((////////////////////////////////////////++++++++++++++++++...
 .++++++++///////////////////////+++..++++//((((((((///////////////////////////////////++++++++++++++++++++ .
 .++++++///////////////////////////++,.,+++++/(((((((((/////////////////////////////+++++++++++++++++++++++,..
 .++++++////////////////////////////+++,.,+++++++/((((((((//////////////////////////++++++++++++++++++++++++..
 .+++++++///////////////////++////////++++,.,+++++++++///////////+////////////////+++++++++++++++++++++++++,..
 ..++++++++++//////////////////////+++++++..+...,+++++++++++++++/++++++++++++++++++++++++++++++++++++++++++,...
  ..++++++++++++///////////////+++++++,...,,,,,.,....,,,,+++++++++++++++++++++++++++++++++++++++++++++++,,,,...
  ...++++++++++++++++++++++++++,,,,...,,,,,,,,,..,,++,,,.,,,,,,,,,,,,,,,,,,+++++++++++++++++++++++++,,,,,,,,..
   ...+++++++++++++++,,,,,,,,....,,,,,,,,,,,,,,,..,,++++++,,,,,,,,,,,,,,,,+++++++++++++++++++++++++,,,,,,,,,..
     ...++++++++++++,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,...,++++++++++++++++++++++++++++++++++++++++++++,,,,,,,,,,...
       ,....,++++++++++++++,,,+++++++,,,,,,,,,,,,,,,,,.,++++++++++++++++++++++++++++++++++++++++++++,,,,,,,,..

*/
#pragma warning restore S125 // Sections of code should not be commented out

public class TeraSyncPlugin : MediatorSubscriberBase, IHostedService
{
    private readonly DalamudUtilService _dalamudUtil;
    private readonly TeraSyncConfigService _teraSyncConfigService;
    private readonly ServerConfigurationManager _serverConfigurationManager;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IServiceScope? _runtimeServiceScope;
    private Task? _launchTask = null;

    public TeraSyncPlugin(ILogger<TeraSyncPlugin> logger, TeraSyncConfigService teraSyncConfigService,
        ServerConfigurationManager serverConfigurationManager,
        DalamudUtilService dalamudUtil,
        IServiceScopeFactory serviceScopeFactory, TeraMediator mediator) : base(logger, mediator)
    {
        _teraSyncConfigService = teraSyncConfigService;
        _serverConfigurationManager = serverConfigurationManager;
        _dalamudUtil = dalamudUtil;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        Logger.LogInformation("Launching {name} {major}.{minor}.{build}", "Tera Sync V2", version.Major, version.Minor, version.Build);
        Mediator.Publish(new EventMessage(new Services.Events.Event(nameof(TeraSyncPlugin), Services.Events.EventSeverity.Informational,
            $"Starting Tera Sync V2 {version.Major}.{version.Minor}.{version.Build}")));

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (msg) => { if (_launchTask == null || _launchTask.IsCompleted) _launchTask = Task.Run(WaitForPlayerAndLaunchCharacterManager); });
        Mediator.Subscribe<DalamudLoginMessage>(this, (_) => DalamudUtilOnLogIn());
        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) => DalamudUtilOnLogOut());

        Mediator.StartQueueProcessing();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        UnsubscribeAll();

        DalamudUtilOnLogOut();

        Logger.LogDebug("Halting TeraSyncPlugin");

        return Task.CompletedTask;
    }

    private void DalamudUtilOnLogIn()
    {
        Logger?.LogDebug("Client login");
        if (_launchTask == null || _launchTask.IsCompleted) _launchTask = Task.Run(WaitForPlayerAndLaunchCharacterManager);
    }

    private void DalamudUtilOnLogOut()
    {
        Logger?.LogDebug("Client logout");

        _runtimeServiceScope?.Dispose();
    }

    private async Task WaitForPlayerAndLaunchCharacterManager()
    {
        while (!await _dalamudUtil.GetIsPlayerPresentAsync().ConfigureAwait(false))
        {
            await Task.Delay(100).ConfigureAwait(false);
        }

        try
        {
            Logger?.LogDebug("Launching Managers");

            _runtimeServiceScope?.Dispose();
            _runtimeServiceScope = _serviceScopeFactory.CreateScope();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<UiService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CommandManagerService>();
            if (!_teraSyncConfigService.Current.HasValidSetup() || !_serverConfigurationManager.HasValidConfig())
            {
                Mediator.Publish(new SwitchToIntroUiMessage());
                return;
            }
            _runtimeServiceScope.ServiceProvider.GetRequiredService<CacheCreationService>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<TransientResourceManager>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<VisibleUserDataDistributor>();
            _runtimeServiceScope.ServiceProvider.GetRequiredService<NotificationService>();

#if !DEBUG
            if (_teraSyncConfigService.Current.LogLevel != LogLevel.Information)
            {
                Mediator.Publish(new NotificationMessage("Abnormal Log Level",
                    $"Your log level is set to '{_teraSyncConfigService.Current.LogLevel}' which is not recommended for normal usage. Set it to '{LogLevel.Information}' in \"Settings -> Debug\" unless instructed otherwise.",
                    TeraSyncConfiguration.Models.NotificationType.Error, TimeSpan.FromSeconds(15000)));
            }
#endif
        }
        catch (Exception ex)
        {
            Logger?.LogCritical(ex, "Error during launch of managers");
        }
    }
}