using TeraSyncV2.TeraSyncConfiguration.Configurations;

namespace TeraSyncV2.TeraSyncConfiguration;

public static class ConfigurationExtensions
{
    public static bool HasValidSetup(this TeraSyncConfig configuration)
    {
        return configuration.AcceptedAgreement && configuration.InitialScanComplete
                    && !string.IsNullOrEmpty(configuration.CacheFolder)
                    && Directory.Exists(configuration.CacheFolder);
    }
}