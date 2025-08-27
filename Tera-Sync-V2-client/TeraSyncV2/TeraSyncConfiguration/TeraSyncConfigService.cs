using TeraSyncV2.TeraSyncConfiguration.Configurations;

namespace TeraSyncV2.TeraSyncConfiguration;

public class TeraSyncConfigService : ConfigurationServiceBase<TeraSyncConfig>
{
    public const string ConfigName = "config.json";

    public TeraSyncConfigService(string configDir) : base(configDir)
    {
    }

    public override string ConfigurationName => ConfigName;
}