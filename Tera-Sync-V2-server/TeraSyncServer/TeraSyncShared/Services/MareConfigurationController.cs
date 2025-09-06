using TeraSyncV2Shared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TeraSyncV2Shared.Services;

[Route("configuration/[controller]")]
[Authorize(Policy = "Internal")]
public class TeraConfigurationController<T> : Controller where T : class, ITeraConfiguration
{
    private readonly ILogger<TeraConfigurationController<T>> _logger;
    private IOptionsMonitor<T> _config;

    public TeraConfigurationController(IOptionsMonitor<T> config, ILogger<TeraConfigurationController<T>> logger)
    {
        _config = config;
        _logger = logger;
    }

    [HttpGet("GetConfigurationEntry")]
    [Authorize(Policy = "Internal")]
    public IActionResult GetConfigurationEntry(string key, string defaultValue)
    {
        var result = _config.CurrentValue.SerializeValue(key, defaultValue);
        _logger.LogInformation("Requested " + key + ", returning:" + result);
        return Ok(result);
    }
}

#pragma warning disable MA0048 // File name must match type name
public class TeraStaticFilesServerConfigurationController : TeraConfigurationController<StaticFilesServerConfiguration>
{
    public TeraStaticFilesServerConfigurationController(IOptionsMonitor<StaticFilesServerConfiguration> config, ILogger<TeraStaticFilesServerConfigurationController> logger) : base(config, logger)
    {
    }
}

public class TeraBaseConfigurationController : TeraConfigurationController<TeraSyncConfigurationBase>
{
    public TeraBaseConfigurationController(IOptionsMonitor<TeraSyncConfigurationBase> config, ILogger<TeraBaseConfigurationController> logger) : base(config, logger)
    {
    }
}

public class TeraServerConfigurationController : TeraConfigurationController<ServerConfiguration>
{
    public TeraServerConfigurationController(IOptionsMonitor<ServerConfiguration> config, ILogger<TeraServerConfigurationController> logger) : base(config, logger)
    {
    }
}

public class TeraServicesConfigurationController : TeraConfigurationController<ServicesConfiguration>
{
    public TeraServicesConfigurationController(IOptionsMonitor<ServicesConfiguration> config, ILogger<TeraServicesConfigurationController> logger) : base(config, logger)
    {
    }
}
#pragma warning restore MA0048 // File name must match type name
