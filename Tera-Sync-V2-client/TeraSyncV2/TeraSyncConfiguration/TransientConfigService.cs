using TeraSyncV2.TeraSyncConfiguration.Configurations;

namespace TeraSyncV2.TeraSyncConfiguration;

public class TransientConfigService : ConfigurationServiceBase<TransientConfig>
{
    public const string ConfigName = "transient.json";

    public TransientConfigService(string configDir) : base(configDir)
    {
    }

    public override string ConfigurationName => ConfigName;
}
