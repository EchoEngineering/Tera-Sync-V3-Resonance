using TeraSyncV2.TeraSyncConfiguration.Configurations;

namespace TeraSyncV2.TeraSyncConfiguration;

public class CharaDataConfigService : ConfigurationServiceBase<CharaDataConfig>
{
    public const string ConfigName = "charadata.json";

    public CharaDataConfigService(string configDir) : base(configDir) { }
    public override string ConfigurationName => ConfigName;
}