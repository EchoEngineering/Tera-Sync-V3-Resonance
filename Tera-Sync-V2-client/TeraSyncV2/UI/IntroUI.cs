using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Microsoft.Extensions.Logging;
using TeraSyncV2.FileCache;
using TeraSyncV2.Localization;
using TeraSyncV2.TeraSyncConfiguration;
using TeraSyncV2.TeraSyncConfiguration.Models;
using TeraSyncV2.Services;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.Services.ServerConfiguration;
using System.Numerics;
using System.Text.RegularExpressions;

namespace TeraSyncV2.UI;

public partial class IntroUi : WindowMediatorSubscriberBase
{
    private readonly TeraSyncConfigService _configService;
    private readonly CacheMonitor _cacheMonitor;
    private readonly Dictionary<string, string> _languages = new(StringComparer.Ordinal) { { "English", "en" }, { "Deutsch", "de" }, { "Français", "fr" } };
    private readonly ServerConfigurationManager _serverConfigurationManager;
    private readonly DalamudUtilService _dalamudUtilService;
    private readonly UiSharedService _uiShared;
    private int _currentLanguage;
    private bool _readFirstPage;

    private string _secretKey = string.Empty;
    private string _timeoutLabel = string.Empty;
    private Task? _timeoutTask;
    private string[]? _tosParagraphs;
    private bool _useLegacyLogin = false;

    public IntroUi(ILogger<IntroUi> logger, UiSharedService uiShared, TeraSyncConfigService configService,
        CacheMonitor fileCacheManager, ServerConfigurationManager serverConfigurationManager, TeraMediator teraMediator,
        PerformanceCollectorService performanceCollectorService, DalamudUtilService dalamudUtilService) : base(logger, teraMediator, "Tera Sync V2 Setup", performanceCollectorService)
    {
        _uiShared = uiShared;
        _configService = configService;
        _cacheMonitor = fileCacheManager;
        _serverConfigurationManager = serverConfigurationManager;
        _dalamudUtilService = dalamudUtilService;
        IsOpen = false;
        ShowCloseButton = false;
        RespectCloseHotkey = false;

        SizeConstraints = new Window.WindowSizeConstraints()
        {
            MinimumSize = new Vector2(600, 400),
            MaximumSize = new Vector2(600, 2000),
        };

        GetToSLocalization();

        Mediator.Subscribe<SwitchToMainUiMessage>(this, (_) => IsOpen = false);
        Mediator.Subscribe<SwitchToIntroUiMessage>(this, (_) =>
        {
            _configService.Current.UseCompactor = !dalamudUtilService.IsWine;
            IsOpen = true;
        });
    }

    private int _prevIdx = -1;

    protected override void DrawInternal()
    {
        if (_uiShared.IsInGpose) return;

        if (!_configService.Current.AcceptedAgreement && !_readFirstPage)
        {
            _uiShared.BigText("Welcome to Tera Sync V2");
            ImGui.Separator();
            UiSharedService.TextWrapped("Tera Sync V2 is a plugin that will replicate your full current character state including all Penumbra mods to other paired Tera Sync users. " +
                              "Note that you will have to have Penumbra as well as Glamourer installed to use this plugin.");
            UiSharedService.TextWrapped("We will have to setup a few things first before you can start using this plugin. Click on next to continue.");

            UiSharedService.ColorTextWrapped("Note: Any modifications you have applied through anything but Penumbra cannot be shared and your character state on other clients " +
                                 "might look broken because of this or others players mods might not apply on your end altogether. " +
                                 "If you want to use this plugin you will have to move your mods to Penumbra.", ImGuiColors.DalamudYellow);
            if (!_uiShared.DrawOtherPluginState()) return;
            ImGui.Separator();
            if (ImGui.Button("Next##toAgreement"))
            {
                _readFirstPage = true;
#if !DEBUG
                _timeoutTask = Task.Run(async () =>
                {
                    for (int i = 10; i > 0; i--)
                    {
                        _timeoutLabel = $"{Strings.ToS.ButtonWillBeAvailableIn} {i}s";
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                    }
                });
#else
                _timeoutTask = Task.CompletedTask;
#endif
            }
        }
        else if (!_configService.Current.AcceptedAgreement && _readFirstPage)
        {
            Vector2 textSize;
            using (_uiShared.UidFont.Push())
            {
                textSize = ImGui.CalcTextSize(Strings.ToS.LanguageLabel);
                ImGui.TextUnformatted(Strings.ToS.AgreementLabel);
            }

            ImGui.SameLine();
            var languageSize = ImGui.CalcTextSize(Strings.ToS.LanguageLabel);
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - languageSize.X - 80);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + textSize.Y / 2 - languageSize.Y / 2);

            ImGui.TextUnformatted(Strings.ToS.LanguageLabel);
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + textSize.Y / 2 - (languageSize.Y + ImGui.GetStyle().FramePadding.Y) / 2);
            ImGui.SetNextItemWidth(80);
            if (ImGui.Combo("", ref _currentLanguage, _languages.Keys.ToArray(), _languages.Count))
            {
                GetToSLocalization(_currentLanguage);
            }

            ImGui.Separator();
            ImGui.SetWindowFontScale(1.5f);
            string readThis = Strings.ToS.ReadLabel;
            textSize = ImGui.CalcTextSize(readThis);
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - textSize.X / 2);
            UiSharedService.ColorText(readThis, ImGuiColors.DalamudRed);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.Separator();

            UiSharedService.TextWrapped(_tosParagraphs![0]);
            UiSharedService.TextWrapped(_tosParagraphs![1]);
            UiSharedService.TextWrapped(_tosParagraphs![2]);
            UiSharedService.TextWrapped(_tosParagraphs![3]);
            UiSharedService.TextWrapped(_tosParagraphs![4]);
            UiSharedService.TextWrapped(_tosParagraphs![5]);

            ImGui.Separator();
            if (_timeoutTask?.IsCompleted ?? true)
            {
                if (ImGui.Button(Strings.ToS.AgreeLabel + "##toSetup"))
                {
                    _configService.Current.AcceptedAgreement = true;
                    _configService.Save();
                }
            }
            else
            {
                UiSharedService.TextWrapped(_timeoutLabel);
            }
        }
        else if (_configService.Current.AcceptedAgreement
                 && (string.IsNullOrEmpty(_configService.Current.CacheFolder)
                     || !_configService.Current.InitialScanComplete
                     || !Directory.Exists(_configService.Current.CacheFolder)))
        {
            using (_uiShared.UidFont.Push())
                ImGui.TextUnformatted("File Storage Setup");

            ImGui.Separator();

            if (!_uiShared.HasValidPenumbraModPath)
            {
                UiSharedService.ColorTextWrapped("You do not have a valid Penumbra path set. Open Penumbra and set up a valid path for the mod directory.", ImGuiColors.DalamudRed);
            }
            else
            {
                UiSharedService.TextWrapped("To avoid downloading files you already have, TeraSync needs to scan your Penumbra mod directory first. " +
                                     "You'll also need to choose a folder where TeraSync can store downloaded character files from other players. " +
                                     "Once you've set the storage folder and the scan is complete, you'll automatically move on to the service registration.");
                UiSharedService.TextWrapped("Note: The initial scan might take a while if you have a lot of mods. Please be patient and let it finish.");
                UiSharedService.ColorTextWrapped("Warning: After this step, don't delete the FileCache.csv file in your Dalamud Plugin Configurations folder. " +
                                          "If you do, TeraSync will have to rescan your entire mod collection next time you launch it.", ImGuiColors.DalamudYellow);
                UiSharedService.ColorTextWrapped("Warning: If the scan seems stuck for a long time, your Penumbra folder might not be configured correctly.", ImGuiColors.DalamudYellow);
                _uiShared.DrawCacheDirectorySetting();
            }

            if (!_cacheMonitor.IsScanRunning && !string.IsNullOrEmpty(_configService.Current.CacheFolder) && _uiShared.HasValidPenumbraModPath && Directory.Exists(_configService.Current.CacheFolder))
            {
                if (ImGui.Button("Start Scan##startScan"))
                {
                    _cacheMonitor.InvokeScan();
                }
            }
            else
            {
                _uiShared.DrawFileScanState();
            }
            if (!_dalamudUtilService.IsWine)
            {
                var useFileCompactor = _configService.Current.UseCompactor;
                if (ImGui.Checkbox("Use File Compactor", ref useFileCompactor))
                {
                    _configService.Current.UseCompactor = useFileCompactor;
                    _configService.Save();
                }
                UiSharedService.ColorTextWrapped("The File Compactor can save a lot of disk space for downloaded files. It uses a bit more CPU when downloading, but makes loading other characters faster. " +
                    "I recommend keeping it enabled. You can always change this in the settings later.", ImGuiColors.DalamudYellow);
            }
        }
        else if (!_uiShared.ApiController.ServerAlive)
        {
            using (_uiShared.UidFont.Push())
                ImGui.TextUnformatted("Service Registration");
            ImGui.Separator();
            int serverIdx = 0;
            var selectedServer = _serverConfigurationManager.GetServerByIndex(serverIdx);

            using (var node = ImRaii.TreeNode("Advanced Options"))
            {
                if (node)
                {
                    serverIdx = _uiShared.DrawServiceSelection(selectOnChange: true, showConnect: false);
                    if (serverIdx != _prevIdx)
                    {
                        _uiShared.ResetOAuthTasksState();
                        _prevIdx = serverIdx;
                    }

                    selectedServer = _serverConfigurationManager.GetServerByIndex(serverIdx);
                    _useLegacyLogin = !selectedServer.UseOAuth2;

                    if (ImGui.Checkbox("Use Legacy Login with Secret Key", ref _useLegacyLogin))
                    {
                        _serverConfigurationManager.GetServerByIndex(serverIdx).UseOAuth2 = !_useLegacyLogin;
                        _serverConfigurationManager.Save();
                    }
                }
            }

            if (_useLegacyLogin)
            {
                var text = "Enter Secret Key";
                var buttonText = "Save";
                var buttonWidth = _secretKey.Length != 64 ? 0 : ImGuiHelpers.GetButtonSize(buttonText).X + ImGui.GetStyle().ItemSpacing.X;
                var textSize = ImGui.CalcTextSize(text);

                ImGuiHelpers.ScaledDummy(5);
                UiSharedService.DrawGroupedCenteredColorText("PLEASE USE DISCORD OAUTH! It's way more secure and you won't have to deal with managing secret keys. " +
                    "Yes, it's a bit annoying to set up the first time, but it's worth it. The main server fully supports it. " +
                    "If you need help with OAuth setup, come ask us on Discord - we're happy to help!", ImGuiColors.DalamudYellow, 500);
                ImGuiHelpers.ScaledDummy(5);

                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(text);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(UiSharedService.GetWindowContentRegionWidth() - ImGui.GetWindowContentRegionMin().X - buttonWidth - textSize.X);
                ImGui.InputText("", ref _secretKey, 64);
                if (_secretKey.Length > 0 && _secretKey.Length != 64)
                {
                    UiSharedService.ColorTextWrapped("Your secret key must be exactly 64 characters long. This isn't your Lodestone login!", ImGuiColors.DalamudRed);
                }
                else if (_secretKey.Length == 64 && !HexRegex().IsMatch(_secretKey))
                {
                    UiSharedService.ColorTextWrapped("Your secret key can only contain the letters A-F and numbers 0-9.", ImGuiColors.DalamudRed);
                }
                else if (_secretKey.Length == 64)
                {
                    ImGui.SameLine();
                    if (ImGui.Button(buttonText))
                    {
                        if (_serverConfigurationManager.CurrentServer == null) _serverConfigurationManager.SelectServer(0);
                        if (!_serverConfigurationManager.CurrentServer!.SecretKeys.Any())
                        {
                            _serverConfigurationManager.CurrentServer!.SecretKeys.Add(_serverConfigurationManager.CurrentServer.SecretKeys.Select(k => k.Key).LastOrDefault() + 1, new SecretKey()
                            {
                                FriendlyName = $"Secret Key added on Setup ({DateTime.Now:yyyy-MM-dd})",
                                Key = _secretKey,
                            });
                            _serverConfigurationManager.AddCurrentCharacterToServer();
                        }
                        else
                        {
                            _serverConfigurationManager.CurrentServer!.SecretKeys[0] = new SecretKey()
                            {
                                FriendlyName = $"Secret Key added on Setup ({DateTime.Now:yyyy-MM-dd})",
                                Key = _secretKey,
                            };
                        }
                        _secretKey = string.Empty;
                        _ = Task.Run(() => _uiShared.ApiController.CreateConnectionsAsync());
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(selectedServer.OAuthToken))
                {
                    UiSharedService.TextWrapped("Before authenticating, you must complete these steps IN ORDER:");
                    UiSharedService.TextWrapped("1. Join our Discord server using the button below");
                    UiSharedService.TextWrapped("2. Get the 'Tera Sync user' role (Authorized users only)");
                    UiSharedService.TextWrapped("3. Go to #tera-sync-authentication channel");
                    UiSharedService.TextWrapped("4. Engage with the bot and follow the registration process");
                    UiSharedService.TextWrapped("5. Come back here and click 'Check if Server supports Discord OAuth2'");
                    UiSharedService.TextWrapped("6. Finally click 'Authenticate with Server'");
                    
                    // Discord button for easy access
                    ImGui.Spacing();
                    if (_uiShared.IconTextButton(FontAwesomeIcon.Comments, "Join Discord Server"))
                    {
                        Util.OpenLink("https://discord.gg/kWVeUZ62SR");
                    }
                    ImGui.Spacing();
                    _uiShared.DrawOAuth(selectedServer);
                }
                else
                {
                    UiSharedService.ColorTextWrapped($"OAuth2 is connected. Linked to: Discord User {_serverConfigurationManager.GetDiscordUserFromToken(selectedServer)}", ImGuiColors.HealerGreen);
                    UiSharedService.TextWrapped("Now click 'Update UIDs' to fetch all your characters from the server.");
                    _uiShared.DrawUpdateOAuthUIDsButton(selectedServer);
                    var playerName = _dalamudUtilService.GetPlayerName();
                    var playerWorld = _dalamudUtilService.GetHomeWorldId();
                    UiSharedService.TextWrapped($"Select which UID you want to use for {_dalamudUtilService.GetPlayerName()}. If you don't see any UIDs, make sure you're logged into the right Discord account. " +
                        $"Need to switch accounts? Hold CTRL and click the unlink button below.");
                    _uiShared.DrawUnlinkOAuthButton(selectedServer);

                    var auth = selectedServer.Authentications.Find(a => string.Equals(a.CharacterName, playerName, StringComparison.Ordinal) && a.WorldId == playerWorld);
                    if (auth == null)
                    {
                        auth = new Authentication()
                        {
                            CharacterName = playerName,
                            WorldId = playerWorld
                        };
                        selectedServer.Authentications.Add(auth);
                        _serverConfigurationManager.Save();
                    }

                    _uiShared.DrawUIDComboForAuthentication(0, auth, selectedServer.ServerUri);

                    using (ImRaii.Disabled(string.IsNullOrEmpty(auth.UID)))
                    {
                        if (_uiShared.IconTextButton(Dalamud.Interface.FontAwesomeIcon.Link, "Connect to Service"))
                        {
                            _ = Task.Run(() => _uiShared.ApiController.CreateConnectionsAsync());
                        }
                    }
                    if (string.IsNullOrEmpty(auth.UID))
                        UiSharedService.AttachToolTip("Select a UID to be able to connect to the service");
                }
            }
        }
        else
        {
            Mediator.Publish(new SwitchToMainUiMessage());
            IsOpen = false;
        }
    }

    private void GetToSLocalization(int changeLanguageTo = -1)
    {
        if (changeLanguageTo != -1)
        {
            _uiShared.LoadLocalization(_languages.ElementAt(changeLanguageTo).Value);
        }

        _tosParagraphs = [Strings.ToS.Paragraph1, Strings.ToS.Paragraph2, Strings.ToS.Paragraph3, Strings.ToS.Paragraph4, Strings.ToS.Paragraph5, Strings.ToS.Paragraph6];
    }

    [GeneratedRegex("^([A-F0-9]{2})+")]
    private static partial Regex HexRegex();
}