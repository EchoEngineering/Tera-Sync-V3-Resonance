using TeraSyncV2.API.Routes;
using TeraSyncV2StaticFilesServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TeraSyncV2StaticFilesServer.Controllers;

[Route(TeraFiles.Distribution)]
public class DistributionController : ControllerBase
{
    private readonly CachedFileProvider _cachedFileProvider;

    public DistributionController(ILogger<DistributionController> logger, CachedFileProvider cachedFileProvider) : base(logger)
    {
        _cachedFileProvider = cachedFileProvider;
    }

    [HttpGet(TeraFiles.Distribution_Get)]
    [Authorize(Policy = "Internal")]
    public async Task<IActionResult> GetFile(string file)
    {
        _logger.LogInformation($"GetFile:{TeraUser}:{file}");

        var fs = await _cachedFileProvider.DownloadAndGetLocalFileInfo(file);
        if (fs == null) return NotFound();

        return PhysicalFile(fs.FullName, "application/octet-stream");
    }
}
