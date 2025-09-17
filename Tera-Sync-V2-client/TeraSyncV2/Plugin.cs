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
    // Baked into TeraSync client at build time - identifies this fork in Resonance federation
    private const string FORK_IDENTIFIER = "TeraSync";

    private readonly IHost _host;
    private readonly IDisposable? _resonance;

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

        // Initialize Resonance Federation
        var configService = _host.Services.GetRequiredService<TeraSyncConfigService>();
        var logger = new DalamudLogger("Resonance", configService, pluginLog, gameData.HasModifiedGameDataFiles);

        // Create Resonance configuration from TeraSync settings
        var resonanceConfig = new ResonanceConfig
        {
            // TeraSync uses federation mode (no Discord auth) - may change based on settings
            EnableDiscordAuthentication = false,
            AggregatorUrl = "https://aggregator.resonancesync.app",
            DisplayName = FORK_IDENTIFIER,
            Description = $"{FORK_IDENTIFIER} cross-fork synchronization via Resonance federation"
        };

        _resonance = ResonanceSDK.Initialize(FORK_IDENTIFIER, resonanceConfig, pluginInterface, commandManager, logger);

        // Initialize hybrid authentication (PKI + bearer tokens)
        _ = Task.Run(async () => await InitializeHybridAuthenticationAsync(pluginInterface, pluginLog).ConfigureAwait(false));
    }

    /// <summary>
    /// Initialize hybrid authentication for cross-fork user token minting.
    /// Loads PKI certificate and enables TeraSync users to get bearer tokens.
    /// </summary>
    private static async Task InitializeHybridAuthenticationAsync(IDalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        try
        {
            var resonanceClient = ResonanceSDK.Client;
            if (resonanceClient == null)
            {
                pluginLog.Warning("[TeraSync-Resonance] Resonance client not available for hybrid auth setup");
                return;
            }

            // Certificate paths (deployed by maintainer or downloaded from portal)
            var configDir = pluginInterface.ConfigDirectory.FullName;
            var certPath = Path.Combine(configDir, "terasync-cert.crt");
            var keyPath = Path.Combine(configDir, "terasync-key.pem");

            // Check if certificate files exist
            if (!File.Exists(certPath) || !File.Exists(keyPath))
            {
                pluginLog.Information("[TeraSync-Resonance] PKI certificate not found - hybrid authentication unavailable");
                pluginLog.Information("[TeraSync-Resonance] Download certificate from maintainer portal to enable user token minting");
                return;
            }

            // Load PKI certificate for fork authentication
            var certLoaded = await resonanceClient.LoadCertificateAsync(certPath, keyPath).ConfigureAwait(false);

            if (certLoaded)
            {
                pluginLog.Information("[TeraSync-Resonance] Hybrid authentication enabled - TeraSync can mint user tokens");
            }
            else
            {
                pluginLog.Error("[TeraSync-Resonance] Failed to load PKI certificate - hybrid authentication disabled");
            }
        }
        catch (Exception ex)
        {
            pluginLog.Error(ex, "[TeraSync-Resonance] Error initializing hybrid authentication");
        }
    }

    /// <summary>
    /// Mint a bearer token for a TeraSync user to access cross-fork features.
    /// Call this when users need to search/sync with other forks.
    /// </summary>
    public static async Task<string?> MintUserTokenAsync(string userId, string userHandle, string? displayName = null)
    {
        try
        {
            var resonanceClient = ResonanceSDK.Client;
            if (resonanceClient == null)
                return null;

            var result = await resonanceClient.MintUserTokenAsync(userId, userHandle, displayName).ConfigureAwait(false);
            return result.Token;
        }
        catch (Exception)
        {
            // Log but don't crash - graceful degradation
            return null;
        }
    }

    public void Dispose()
    {
        _resonance?.Dispose();
        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
    }
}