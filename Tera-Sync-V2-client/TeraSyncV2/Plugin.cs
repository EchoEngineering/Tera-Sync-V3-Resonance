using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NReco.Logging.File;
using System.Net.Http.Headers;
using System.Reflection;
using Dalamud.Game;
using TeraSyncV2.FileCache;
using TeraSyncV2.Interop;
using TeraSyncV2.Interop.Ipc;
using TeraSyncV2.TeraSyncConfiguration;
using TeraSyncV2.TeraSyncConfiguration.Configurations;
using TeraSyncV2.PlayerData.Factories;
using TeraSyncV2.PlayerData.Pairs;
using TeraSyncV2.PlayerData.Services;
using TeraSyncV2.Services;
using TeraSyncV2.Services.CharaData;
using TeraSyncV2.Services.Events;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.Services.ServerConfiguration;
using TeraSyncV2.UI;
using TeraSyncV2.UI.Components;
using TeraSyncV2.UI.Components.Popup;
using TeraSyncV2.UI.Handlers;
using TeraSyncV2.WebAPI;
using TeraSyncV2.WebAPI.Files;
using TeraSyncV2.WebAPI.SignalR;
using Resonance.SDK;

namespace TeraSyncV2;

public sealed class Plugin : IDalamudPlugin
{
    private readonly IHost _host;
    private readonly IResonanceClient _resonanceClient;
    private readonly IDisposable? _resonanceUi;

    public Plugin(IDalamudPluginInterface pluginInterface, ICommandManager commandManager, IDataManager gameData,
        IFramework framework, IObjectTable objectTable, IClientState clientState, ICondition condition, IChatGui chatGui,
        IGameGui gameGui, IDtrBar dtrBar, IPluginLog pluginLog, ITargetManager targetManager, INotificationManager notificationManager,
        ITextureProvider textureProvider, IContextMenu contextMenu, IGameInteropProvider gameInteropProvider, IGameConfig gameConfig,
        ISigScanner sigScanner)
    {
        pluginLog.Information("[TeraSync] Plugin constructor started!");
        
        if (!Directory.Exists(pluginInterface.ConfigDirectory.FullName))
            Directory.CreateDirectory(pluginInterface.ConfigDirectory.FullName);
        var traceDir = Path.Join(pluginInterface.ConfigDirectory.FullName, "tracelog");
        if (!Directory.Exists(traceDir))
            Directory.CreateDirectory(traceDir);

        foreach (var file in Directory.EnumerateFiles(traceDir)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTimeUtc).Skip(9))
        {
            int attempts = 0;
            bool deleted = false;
            while (!deleted && attempts < 5)
            {
                try
                {
                    file.Delete();
                    deleted = true;
                }
                catch
                {
                    attempts++;
                    Thread.Sleep(500);
                }
            }
        }

        _host = new HostBuilder()
        .UseContentRoot(pluginInterface.ConfigDirectory.FullName)
        .ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddDalamudLogging(pluginLog, gameData.HasModifiedGameDataFiles);
            lb.AddFile(Path.Combine(traceDir, $"tera-trace-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log"), (opt) =>
            {
                opt.Append = true;
                opt.RollingFilesConvention = FileLoggerOptions.FileRollingConvention.Ascending;
                opt.MinLevel = LogLevel.Trace;
                opt.FileSizeLimitBytes = 50 * 1024 * 1024;
            });
            lb.SetMinimumLevel(LogLevel.Trace);
        })
        .ConfigureServices(collection =>
        {
            collection.AddSingleton(new WindowSystem("TeraSyncV2"));
            collection.AddSingleton<FileDialogManager>();
            collection.AddSingleton(new Dalamud.Localization("TeraSyncV2.Localization.", "", useEmbedded: true));

            // add tera sync v2 related singletons
            collection.AddSingleton<TeraMediator>();
            collection.AddSingleton<FileCacheManager>();
            collection.AddSingleton<ServerConfigurationManager>();
            collection.AddSingleton<ApiController>();
            collection.AddSingleton<PerformanceCollectorService>();
            collection.AddSingleton<HubFactory>();
            collection.AddSingleton<FileUploadManager>();
            collection.AddSingleton<FileTransferOrchestrator>();
            collection.AddSingleton<TeraSyncPlugin>();
            collection.AddSingleton<TeraProfileManager>();
            collection.AddSingleton<GameObjectHandlerFactory>();
            collection.AddSingleton<FileDownloadManagerFactory>();
            collection.AddSingleton<PairHandlerFactory>();
            collection.AddSingleton<PairFactory>();
            collection.AddSingleton<XivDataAnalyzer>();
            collection.AddSingleton<CharacterAnalyzer>();
            collection.AddSingleton<TokenProvider>();
            collection.AddSingleton<PluginWarningNotificationService>();
            collection.AddSingleton<FileCompactor>();
            collection.AddSingleton<TagHandler>();
            collection.AddSingleton<IdDisplayHandler>();
            collection.AddSingleton<PlayerPerformanceService>();
            collection.AddSingleton<TransientResourceManager>();

            collection.AddSingleton((s) => new CharaDataManager(
                s.GetRequiredService<ILogger<CharaDataManager>>(),
                s.GetRequiredService<ApiController>(),
                s.GetRequiredService<CharaDataFileHandler>(),
                s.GetRequiredService<TeraMediator>(),
                s.GetRequiredService<IpcManager>(),
                s.GetRequiredService<DalamudUtilService>(),
                s.GetRequiredService<FileDownloadManagerFactory>(),
                s.GetRequiredService<CharaDataConfigService>(),
                s.GetRequiredService<CharaDataNearbyManager>(),
                s.GetRequiredService<CharaDataCharacterHandler>(),
                s.GetRequiredService<PairManager>(),
                pluginInterface));
            collection.AddSingleton<CharaDataFileHandler>();
            collection.AddSingleton<CharaDataCharacterHandler>();
            collection.AddSingleton<CharaDataNearbyManager>();
            collection.AddSingleton<CharaDataGposeTogetherManager>();

            collection.AddSingleton(s => new VfxSpawnManager(s.GetRequiredService<ILogger<VfxSpawnManager>>(),
                gameInteropProvider, s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new BlockedCharacterHandler(s.GetRequiredService<ILogger<BlockedCharacterHandler>>(), gameInteropProvider));
            collection.AddSingleton((s) => new IpcProvider(s.GetRequiredService<ILogger<IpcProvider>>(),
                pluginInterface,
                s.GetRequiredService<CharaDataManager>(),
                s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton<SelectPairForTagUi>();
            collection.AddSingleton((s) => new EventAggregator(pluginInterface.ConfigDirectory.FullName,
                s.GetRequiredService<ILogger<EventAggregator>>(), s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new DalamudUtilService(s.GetRequiredService<ILogger<DalamudUtilService>>(),
                clientState, objectTable, framework, gameGui, condition, gameData, targetManager, gameConfig,
                s.GetRequiredService<BlockedCharacterHandler>(), s.GetRequiredService<TeraMediator>(), s.GetRequiredService<PerformanceCollectorService>(),
                s.GetRequiredService<TeraSyncConfigService>()));
            collection.AddSingleton((s) => new DtrEntry(s.GetRequiredService<ILogger<DtrEntry>>(), dtrBar, s.GetRequiredService<TeraSyncConfigService>(),
                s.GetRequiredService<TeraMediator>(), s.GetRequiredService<PairManager>(), s.GetRequiredService<ApiController>()));
            collection.AddSingleton(s => new PairManager(s.GetRequiredService<ILogger<PairManager>>(), s.GetRequiredService<PairFactory>(),
                s.GetRequiredService<TeraSyncConfigService>(), s.GetRequiredService<TeraMediator>(), contextMenu));
            collection.AddSingleton<RedrawManager>();
            collection.AddSingleton((s) => new IpcCallerPenumbra(s.GetRequiredService<ILogger<IpcCallerPenumbra>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>(), s.GetRequiredService<RedrawManager>()));
            collection.AddSingleton((s) => new IpcCallerGlamourer(s.GetRequiredService<ILogger<IpcCallerGlamourer>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>(), s.GetRequiredService<RedrawManager>()));
            collection.AddSingleton((s) => new IpcCallerCustomize(s.GetRequiredService<ILogger<IpcCallerCustomize>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new IpcCallerHeels(s.GetRequiredService<ILogger<IpcCallerHeels>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new IpcCallerHonorific(s.GetRequiredService<ILogger<IpcCallerHonorific>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new IpcCallerMoodles(s.GetRequiredService<ILogger<IpcCallerMoodles>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new IpcCallerPetNames(s.GetRequiredService<ILogger<IpcCallerPetNames>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>(), s.GetRequiredService<TeraMediator>()));
            collection.AddSingleton((s) => new IpcCallerBrio(s.GetRequiredService<ILogger<IpcCallerBrio>>(), pluginInterface,
                s.GetRequiredService<DalamudUtilService>()));
            collection.AddSingleton((s) => new IpcManager(s.GetRequiredService<ILogger<IpcManager>>(),
                s.GetRequiredService<TeraMediator>(), s.GetRequiredService<IpcCallerPenumbra>(), s.GetRequiredService<IpcCallerGlamourer>(),
                s.GetRequiredService<IpcCallerCustomize>(), s.GetRequiredService<IpcCallerHeels>(), s.GetRequiredService<IpcCallerHonorific>(),
                s.GetRequiredService<IpcCallerMoodles>(), s.GetRequiredService<IpcCallerPetNames>(), s.GetRequiredService<IpcCallerBrio>()));
            collection.AddSingleton((s) => new NotificationService(s.GetRequiredService<ILogger<NotificationService>>(),
                s.GetRequiredService<TeraMediator>(), s.GetRequiredService<DalamudUtilService>(),
                notificationManager, chatGui, s.GetRequiredService<TeraSyncConfigService>()));
            collection.AddSingleton((s) =>
            {
                var httpClient = new HttpClient();
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("TeraSyncV2", ver!.Major + "." + ver!.Minor + "." + ver!.Build));
                return httpClient;
            });
            collection.AddSingleton((s) => new TeraSyncConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new ServerConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new NotesConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new ServerTagConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new TransientConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new XivDataStorageService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new PlayerPerformanceConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton((s) => new CharaDataConfigService(pluginInterface.ConfigDirectory.FullName));
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<TeraSyncConfigService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<ServerConfigService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<NotesConfigService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<ServerTagConfigService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<TransientConfigService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<XivDataStorageService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<PlayerPerformanceConfigService>());
            collection.AddSingleton<IConfigService<ITeraSyncConfiguration>>(s => s.GetRequiredService<CharaDataConfigService>());
            collection.AddSingleton<ConfigurationMigrator>();
            collection.AddSingleton<ConfigurationSaveService>();

            collection.AddSingleton<HubFactory>();

            // add scoped services
            collection.AddScoped<DrawEntityFactory>();
            collection.AddScoped<CacheMonitor>();
            collection.AddScoped<UiFactory>();
            collection.AddScoped<SelectTagForPairUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, SettingsUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, CompactUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, IntroUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, DownloadUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, PopoutProfileUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, DataAnalysisUi>();
            collection.AddScoped<WindowMediatorSubscriberBase, JoinSyncshellUI>();
            collection.AddScoped<WindowMediatorSubscriberBase, CreateSyncshellUI>();
            collection.AddScoped<WindowMediatorSubscriberBase, EventViewerUI>();
            collection.AddScoped<WindowMediatorSubscriberBase, CharaDataHubUi>();

            collection.AddScoped<WindowMediatorSubscriberBase, EditProfileUi>((s) => new EditProfileUi(s.GetRequiredService<ILogger<EditProfileUi>>(),
                s.GetRequiredService<TeraMediator>(), s.GetRequiredService<ApiController>(), s.GetRequiredService<UiSharedService>(), s.GetRequiredService<FileDialogManager>(),
                s.GetRequiredService<TeraProfileManager>(), s.GetRequiredService<PerformanceCollectorService>()));
            collection.AddScoped<WindowMediatorSubscriberBase, PopupHandler>();
            collection.AddScoped<IPopupHandler, BanUserPopupHandler>();
            collection.AddScoped<IPopupHandler, CensusPopupHandler>();
            collection.AddScoped<CacheCreationService>();
            collection.AddScoped<PlayerDataFactory>();
            collection.AddScoped<VisibleUserDataDistributor>();
            collection.AddScoped((s) => new UiService(s.GetRequiredService<ILogger<UiService>>(), pluginInterface.UiBuilder, s.GetRequiredService<TeraSyncConfigService>(),
                s.GetRequiredService<WindowSystem>(), s.GetServices<WindowMediatorSubscriberBase>(),
                s.GetRequiredService<UiFactory>(),
                s.GetRequiredService<FileDialogManager>(), s.GetRequiredService<TeraMediator>()));
            collection.AddScoped((s) => new CommandManagerService(commandManager, s.GetRequiredService<PerformanceCollectorService>(),
                s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<CacheMonitor>(), s.GetRequiredService<ApiController>(),
                s.GetRequiredService<TeraMediator>(), s.GetRequiredService<TeraSyncConfigService>()));
            collection.AddScoped((s) => new UiSharedService(s.GetRequiredService<ILogger<UiSharedService>>(), s.GetRequiredService<IpcManager>(), s.GetRequiredService<ApiController>(),
                s.GetRequiredService<CacheMonitor>(), s.GetRequiredService<FileDialogManager>(), s.GetRequiredService<TeraSyncConfigService>(), s.GetRequiredService<DalamudUtilService>(),
                pluginInterface, textureProvider, s.GetRequiredService<Dalamud.Localization>(), s.GetRequiredService<ServerConfigurationManager>(), s.GetRequiredService<TokenProvider>(),
                s.GetRequiredService<TeraMediator>()));

            collection.AddHostedService(p => p.GetRequiredService<ConfigurationSaveService>());
            collection.AddHostedService(p => p.GetRequiredService<TeraMediator>());
            collection.AddHostedService(p => p.GetRequiredService<NotificationService>());
            collection.AddHostedService(p => p.GetRequiredService<FileCacheManager>());
            collection.AddHostedService(p => p.GetRequiredService<ConfigurationMigrator>());
            collection.AddHostedService(p => p.GetRequiredService<DalamudUtilService>());
            collection.AddHostedService(p => p.GetRequiredService<PerformanceCollectorService>());
            collection.AddHostedService(p => p.GetRequiredService<DtrEntry>());
            collection.AddHostedService(p => p.GetRequiredService<EventAggregator>());
            collection.AddHostedService(p => p.GetRequiredService<IpcProvider>());
            collection.AddHostedService(p => p.GetRequiredService<TeraSyncPlugin>());
        })
        .Build();

        _ = _host.StartAsync();
        
        // Initialize Resonance SDK for cross-fork federation
        pluginLog.Debug("[Resonance] === Starting Resonance SDK Integration ===");
        pluginLog.Debug("[Resonance] Plugin interface valid: {0}", pluginInterface != null);
        pluginLog.Debug("[Resonance] Command manager valid: {0}", commandManager != null);
        pluginLog.Debug("[Resonance] Config directory: {0}", pluginInterface.ConfigDirectory.FullName);
        
        try
        {
            // Create Resonance client with embedded PDS
            var resonanceConfig = new ResonanceConfig
            {
                DatabasePath = Path.Combine(pluginInterface.ConfigDirectory.FullName, "resonance.db"),
                EnableDebugLogging = true, // Enable debug logging to see SDK internals
                
                // Required for federation registration - TeraSync as primary/admin fork
                AggregatorRegistrationKey = "terasync-admin-key-2025", // Admin registration key
                ContactEmail = "admin@terasync.app",
                PdsEndpoint = "embedded", // Use embedded PDS for simplicity
                DisplayName = "TeraSync",
                Description = "Primary TeraSync client - official Resonance federation hub",
                
                // Optional configuration - TeraSync as flagship fork
                MaxUsers = 10000,
                SupportedFeatures = new[] { "character_sync", "mod_federation", "cross_fork_discovery", "admin_controls" }
            };
            
            pluginLog.Debug("[Resonance] Config created:");
            pluginLog.Debug("[Resonance]   - DatabasePath: {0}", resonanceConfig.DatabasePath);
            pluginLog.Debug("[Resonance]   - EnableDebugLogging: {0}", resonanceConfig.EnableDebugLogging);
            pluginLog.Debug("[Resonance]   - DisplayName: {0}", resonanceConfig.DisplayName);
            pluginLog.Debug("[Resonance]   - PdsEndpoint: {0}", resonanceConfig.PdsEndpoint);
            
            pluginLog.Information("[Resonance] Creating ResonanceClient instance");
            _resonanceClient = new ResonanceClient(resonanceConfig);
            pluginLog.Information("[Resonance] ResonanceClient instance created successfully");
            pluginLog.Debug("[Resonance] ResonanceClient type: {0}", _resonanceClient?.GetType().FullName ?? "null");
            
            // Initialize federation for TeraSync in background
            pluginLog.Debug("[Resonance] Starting async initialization task");
            Task.Run(async () =>
            {
                try
                {
                    pluginLog.Debug("[Resonance] Async task started - calling InitializeAsync");
                    var success = await _resonanceClient.InitializeAsync("TeraSync");
                    pluginLog.Information("[Resonance] InitializeAsync returned: {0}", success);
                    
                    if (success)
                    {
                        pluginLog.Debug("[Resonance] Initialization successful, enabling Dalamud integration");
                        // Enable IPC integration so TeraSync's existing calls to Resonance.PublishData work
                        var ipcIntegration = _resonanceClient.EnableDalamudIntegration(pluginInterface);
                        pluginLog.Information("[Resonance] IPC integration enabled - result: {0}", ipcIntegration != null);
                    }
                    else
                    {
                        pluginLog.Warning("[Resonance] InitializeAsync failed - federation not enabled");
                    }
                }
                catch (Exception ex)
                {
                    pluginLog.Error(ex, "[Resonance] Exception in async initialization");
                }
            });
            
            // Register the UI - adds /resonance and /res commands  
            pluginLog.Information("[Resonance] Creating UI integration");
            pluginLog.Debug("[Resonance] Calling CreateUIIntegration with fork name: TeraSync");
            
            _resonanceUi = _resonanceClient.CreateUIIntegration("TeraSync", 
                (command, action) =>
                {
                    pluginLog.Information("[Resonance] Command registration callback invoked for: /{0}", command);
                    pluginLog.Debug("[Resonance] Action delegate is null: {0}", action == null);
                    
                    var commandInfo = new CommandInfo((cmd, args) => {
                        pluginLog.Information("[Resonance] === Command Execution Started ===");
                        pluginLog.Information("[Resonance] Command: {0}, Args: {1}", cmd, args ?? "(none)");
                        pluginLog.Debug("[Resonance] Thread ID: {0}", System.Threading.Thread.CurrentThread.ManagedThreadId);
                        pluginLog.Debug("[Resonance] Action delegate about to be invoked");
                        
                        try 
                        {
                            if (action == null)
                            {
                                pluginLog.Error("[Resonance] Action delegate is null - cannot execute command");
                                return;
                            }
                            
                            pluginLog.Debug("[Resonance] Invoking action delegate");
                            action();
                            pluginLog.Information("[Resonance] Action delegate completed without exception");
                        }
                        catch (Exception ex)
                        {
                            pluginLog.Error(ex, "[Resonance] Exception during command execution");
                            pluginLog.Error("[Resonance] Exception type: {0}", ex.GetType().FullName);
                            pluginLog.Error("[Resonance] Exception message: {0}", ex.Message);
                            pluginLog.Error("[Resonance] Stack trace: {0}", ex.StackTrace);
                        }
                        finally
                        {
                            pluginLog.Information("[Resonance] === Command Execution Ended ===");
                        }
                    })
                    {
                        HelpMessage = "Open Resonance Federation UI",
                        ShowInHelp = true
                    };
                    
                    pluginLog.Debug("[Resonance] Adding handler for command: /{0}", command);
                    commandManager.AddHandler($"/{command}", commandInfo);
                    pluginLog.Information("[Resonance] Command /{0} registered successfully", command);
                },
                pluginInterface.UiBuilder,
                pluginLog // Pass the IPluginLog to the SDK
            );
            
            pluginLog.Information("[Resonance] UI integration result: {0}", _resonanceUi != null ? "Success" : "Failed");
            pluginLog.Debug("[Resonance] UI integration type: {0}", _resonanceUi?.GetType().FullName ?? "null");
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "[Resonance] Failed to set up SDK");
            pluginLog.Error("[Resonance] Exception type: {0}", ex.GetType().FullName);
            pluginLog.Error("[Resonance] Exception message: {0}", ex.Message);
            pluginLog.Error("[Resonance] Stack trace: {0}", ex.StackTrace);
        }
        finally
        {
            pluginLog.Debug("[Resonance] === SDK Integration Complete ===");
        }
    }

    public void Dispose()
    {
        _resonanceUi?.Dispose();
        (_resonanceClient as IDisposable)?.Dispose();
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}