using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Microsoft.Extensions.Logging;
using TeraSyncV2.Services;
using TeraSyncV2.Services.Mediator;

namespace TeraSyncV2.Interop.Ipc;

public sealed class IpcCallerMoodles : IIpcCaller
{
    private readonly ICallGateSubscriber<int> _moodlesApiVersion;
    private readonly ICallGateSubscriber<IPlayerCharacter, object> _moodlesOnChange;
    private readonly ICallGateSubscriber<nint, string> _moodlesGetStatus;
    private readonly ICallGateSubscriber<nint, string, object> _moodlesSetStatus;
    private readonly ICallGateSubscriber<nint, object> _moodlesRevertStatus;
    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly DalamudUtilService _dalamudUtil;
    private readonly TeraMediator _teraMediator;

    public IpcCallerMoodles(ILogger<IpcCallerMoodles> logger, IDalamudPluginInterface pi, DalamudUtilService dalamudUtil,
        TeraMediator teraMediator)
    {
        _logger = logger;
        _dalamudUtil = dalamudUtil;
        _teraMediator = teraMediator;

        _moodlesApiVersion = pi.GetIpcSubscriber<int>("Moodles.Version");
        _moodlesOnChange = pi.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        _moodlesGetStatus = pi.GetIpcSubscriber<nint, string>("Moodles.GetStatusManagerByPtrV2");
        _moodlesSetStatus = pi.GetIpcSubscriber<nint, string, object>("Moodles.SetStatusManagerByPtrV2");
        _moodlesRevertStatus = pi.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtrV2");

        _moodlesOnChange.Subscribe(OnMoodlesChange);
        _logger.LogDebug("TeraSync subscribed to Moodles.StatusManagerModified IPC event");

        CheckAPI();
        _logger.LogDebug("Moodles API initialized. Available: {available}", APIAvailable);
    }

    private void OnMoodlesChange(IPlayerCharacter character)
    {
        _logger.LogDebug("TeraSync received Moodles change for character: {name} at address {address}", 
            character.Name.TextValue, character.Address);
        _teraMediator.Publish(new MoodlesMessage(character.Address));
    }

    public bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            APIAvailable = _moodlesApiVersion.InvokeFunc() == 3;
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        _moodlesOnChange.Unsubscribe(OnMoodlesChange);
    }

    public async Task<string?> GetStatusAsync(nint address)
    {
        if (!APIAvailable) return null;

        try
        {
            return await _dalamudUtil.RunOnFrameworkThread(() => _moodlesGetStatus.InvokeFunc(address)).ConfigureAwait(false);

        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Get Moodles Status");
            return null;
        }
    }

    public async Task SetStatusAsync(nint pointer, string status)
    {
        if (!APIAvailable) 
        {
            _logger.LogDebug("Moodles API not available, cannot set status for pointer {pointer}", pointer);
            return;
        }
        try
        {
            _logger.LogDebug("Setting Moodles status for pointer {pointer}: Length={length}, Data={data}", 
                pointer, status?.Length ?? 0, 
                string.IsNullOrEmpty(status) ? "EMPTY" : status.Substring(0, Math.Min(50, status.Length)) + "...");
            await _dalamudUtil.RunOnFrameworkThread(() => _moodlesSetStatus.InvokeAction(pointer, status)).ConfigureAwait(false);
            _logger.LogDebug("Successfully set Moodles status for pointer {pointer}", pointer);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Set Moodles Status for pointer {pointer}", pointer);
        }
    }

    public async Task RevertStatusAsync(nint pointer)
    {
        if (!APIAvailable) return;
        try
        {
            await _dalamudUtil.RunOnFrameworkThread(() => _moodlesRevertStatus.InvokeAction(pointer)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Could not Set Moodles Status");
        }
    }
}
