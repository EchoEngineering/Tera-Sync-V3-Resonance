using Dalamud.Plugin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TeraSyncV2.TeraSyncConfiguration;

namespace TeraSyncV2.Interop;

public static class DalamudLoggingProviderExtensions
{
    public static ILoggingBuilder AddDalamudLogging(this ILoggingBuilder builder, IPluginLog pluginLog, bool hasModifiedGameFiles)
    {
        builder.ClearProviders();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, DalamudLoggingProvider>
            (b => new DalamudLoggingProvider(b.GetRequiredService<TeraSyncConfigService>(), pluginLog, hasModifiedGameFiles)));
        return builder;
    }
}