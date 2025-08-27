using Dalamud.Game.ClientState.Objects.Types;
using TeraSyncV2.Utils;
using Microsoft.Extensions.Logging;
using TeraSyncV2.PlayerData.Handlers;
using TeraSyncV2.Services;
using TeraSyncV2.Services.Mediator;
using System.Collections.Concurrent;

namespace TeraSyncV2.Interop.Ipc;

public class RedrawManager
{
    private readonly TeraMediator _teraMediator;
    private readonly DalamudUtilService _dalamudUtil;
    private readonly ConcurrentDictionary<nint, bool> _penumbraRedrawRequests = [];
    private CancellationTokenSource _disposalCts = new();

    public SemaphoreSlim RedrawSemaphore { get; init; } = new(2, 2);

    public RedrawManager(TeraMediator teraMediator, DalamudUtilService dalamudUtil)
    {
        _teraMediator = teraMediator;
        _dalamudUtil = dalamudUtil;
    }

    public async Task PenumbraRedrawInternalAsync(ILogger logger, GameObjectHandler handler, Guid applicationId, Action<ICharacter> action, CancellationToken token)
    {
        _teraMediator.Publish(new PenumbraStartRedrawMessage(handler.Address));

        _penumbraRedrawRequests[handler.Address] = true;

        try
        {
            using CancellationTokenSource cancelToken = new CancellationTokenSource();
            using CancellationTokenSource combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancelToken.Token, token, _disposalCts.Token);
            var combinedToken = combinedCts.Token;
            cancelToken.CancelAfter(TimeSpan.FromSeconds(15));
            await handler.ActOnFrameworkAfterEnsureNoDrawAsync(action, combinedToken).ConfigureAwait(false);

            if (!_disposalCts.Token.IsCancellationRequested)
                await _dalamudUtil.WaitWhileCharacterIsDrawing(logger, handler, applicationId, 30000, combinedToken).ConfigureAwait(false);
        }
        finally
        {
            _penumbraRedrawRequests[handler.Address] = false;
            _teraMediator.Publish(new PenumbraEndRedrawMessage(handler.Address));
        }
    }

    internal void Cancel()
    {
        _disposalCts = _disposalCts.CancelRecreate();
    }
}
