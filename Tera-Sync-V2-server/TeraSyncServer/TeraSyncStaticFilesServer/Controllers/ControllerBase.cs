using TeraSyncV2Shared.Utils;
using Microsoft.AspNetCore.Mvc;

namespace TeraSyncV2StaticFilesServer.Controllers;

public class ControllerBase : Controller
{
    protected ILogger _logger;

    public ControllerBase(ILogger logger)
    {
        _logger = logger;
    }

    protected string TeraUser => HttpContext.User.Claims.First(f => string.Equals(f.Type, TeraClaimTypes.Uid, StringComparison.Ordinal)).Value;
    protected string Continent => HttpContext.User.Claims.FirstOrDefault(f => string.Equals(f.Type, TeraClaimTypes.Continent, StringComparison.Ordinal))?.Value ?? "*";
    protected bool IsPriority => !string.IsNullOrEmpty(HttpContext.User.Claims.FirstOrDefault(f => string.Equals(f.Type, TeraClaimTypes.Alias, StringComparison.Ordinal))?.Value ?? string.Empty);
}
