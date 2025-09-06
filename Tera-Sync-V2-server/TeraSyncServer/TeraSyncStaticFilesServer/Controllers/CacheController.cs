using TeraSyncV2.API.Routes;
using TeraSyncV2StaticFilesServer.Services;
using TeraSyncV2StaticFilesServer.Utils;
using Microsoft.AspNetCore.Mvc;

namespace TeraSyncV2StaticFilesServer.Controllers;

[Route(TeraFiles.Cache)]
public class CacheController : ControllerBase
{
    private readonly RequestFileStreamResultFactory _requestFileStreamResultFactory;
    private readonly CachedFileProvider _cachedFileProvider;
    private readonly RequestQueueService _requestQueue;
    private readonly FileStatisticsService _fileStatisticsService;

    public CacheController(ILogger<CacheController> logger, RequestFileStreamResultFactory requestFileStreamResultFactory,
        CachedFileProvider cachedFileProvider, RequestQueueService requestQueue, FileStatisticsService fileStatisticsService) : base(logger)
    {
        _requestFileStreamResultFactory = requestFileStreamResultFactory;
        _cachedFileProvider = cachedFileProvider;
        _requestQueue = requestQueue;
        _fileStatisticsService = fileStatisticsService;
    }

    [HttpGet(TeraFiles.Cache_Get)]
    public async Task<IActionResult> GetFiles(Guid requestId)
    {
        _logger.LogDebug($"GetFile:{TeraUser}:{requestId}");

        if (!_requestQueue.IsActiveProcessing(requestId, TeraUser, out var request)) return BadRequest();

        _requestQueue.ActivateRequest(requestId);

        Response.ContentType = "application/octet-stream";

        long requestSize = 0;
        List<BlockFileDataSubstream> substreams = new();

        foreach (var fileHash in request.FileIds)
        {
            var fs = await _cachedFileProvider.DownloadAndGetLocalFileInfo(fileHash).ConfigureAwait(false);
            if (fs == null) continue;

            substreams.Add(new(fs));

            requestSize += fs.Length;
        }

        _fileStatisticsService.LogRequest(requestSize);

        return _requestFileStreamResultFactory.Create(requestId, new BlockFileDataStream(substreams));
    }
}
