using TeraSyncV2.API.Routes;
using TeraSyncV2Shared.Utils.Configuration;
using TeraSyncV2StaticFilesServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TeraSyncV2StaticFilesServer.Controllers;

[Route(TeraFiles.Main)]
[Authorize(Policy = "Internal")]
public class MainController : ControllerBase
{
    private readonly IClientReadyMessageService _messageService;
    private readonly MainServerShardRegistrationService _shardRegistrationService;

    public MainController(ILogger<MainController> logger, IClientReadyMessageService teraHub,
        MainServerShardRegistrationService shardRegistrationService) : base(logger)
    {
        _messageService = teraHub;
        _shardRegistrationService = shardRegistrationService;
    }

    [HttpGet(TeraFiles.Main_SendReady)]
    public async Task<IActionResult> SendReadyToClients(string uid, Guid requestId)
    {
        await _messageService.SendDownloadReady(uid, requestId).ConfigureAwait(false);
        return Ok();
    }

    [HttpPost("shardRegister")]
    public IActionResult RegisterShard([FromBody] ShardConfiguration shardConfiguration)
    {
        try
        {
            _shardRegistrationService.RegisterShard(TeraUser, shardConfiguration);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shard could not be registered {shard}", TeraUser);
            return BadRequest();
        }
    }

    [HttpPost("shardUnregister")]
    public IActionResult UnregisterShard()
    {
        _shardRegistrationService.UnregisterShard(TeraUser);
        return Ok();
    }

    [HttpPost("shardHeartbeat")]
    public IActionResult ShardHeartbeat()
    {
        try
        {
            _shardRegistrationService.ShardHeartbeat(TeraUser);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shard not registered: {shard}", TeraUser);
            return BadRequest();
        }
    }
}