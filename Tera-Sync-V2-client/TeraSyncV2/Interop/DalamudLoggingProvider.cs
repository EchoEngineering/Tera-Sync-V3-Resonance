using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;
using TeraSyncV2.TeraSyncConfiguration;
using System.Collections.Concurrent;

namespace TeraSyncV2.Interop;

[ProviderAlias("Dalamud")]
public sealed class DalamudLoggingProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, DalamudLogger> _loggers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly TeraSyncConfigService _teraSyncConfigService;
    private readonly IPluginLog _pluginLog;
    private readonly bool _hasModifiedGameFiles;

    public DalamudLoggingProvider(TeraSyncConfigService teraConfigService, IPluginLog pluginLog, bool hasModifiedGameFiles)
    {
        _teraSyncConfigService = teraConfigService;
        _pluginLog = pluginLog;
        _hasModifiedGameFiles = hasModifiedGameFiles;
    }

    public ILogger CreateLogger(string categoryName)
    {
        string catName = categoryName.Split(".", StringSplitOptions.RemoveEmptyEntries).Last();
        if (catName.Length > 15)
        {
            catName = string.Join("", catName.Take(6)) + "..." + string.Join("", catName.TakeLast(6));
        }
        else
        {
            catName = string.Join("", Enumerable.Range(0, 15 - catName.Length).Select(_ => " ")) + catName;
        }

        return _loggers.GetOrAdd(catName, name => new DalamudLogger(name, _teraSyncConfigService, _pluginLog, _hasModifiedGameFiles));
    }

    public void Dispose()
    {
        _loggers.Clear();
        GC.SuppressFinalize(this);
    }
}