using Microsoft.AspNetCore.SignalR;
using TeraSyncV2.API.SignalR;
using TeraSyncV2Server.Hubs;

namespace TeraSyncV2StaticFilesServer.Services;

public class MainClientReadyMessageService : IClientReadyMessageService
{
    private readonly ILogger<MainClientReadyMessageService> _logger;
    private readonly IHubContext<TeraHub> _teraHub;

    public MainClientReadyMessageService(ILogger<MainClientReadyMessageService> logger, IHubContext<TeraHub> teraHub)
    {
        _logger = logger;
        _teraHub = teraHub;
    }

    public async Task SendDownloadReady(string uid, Guid requestId)
    {
        _logger.LogInformation("Sending Client Ready for {uid}:{requestId} to SignalR", uid, requestId);
        await _teraHub.Clients.User(uid).SendAsync(nameof(ITeraHub.Client_DownloadReady), requestId).ConfigureAwait(false);
    }
}
