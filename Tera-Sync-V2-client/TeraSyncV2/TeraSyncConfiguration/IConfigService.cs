using TeraSyncV2.TeraSyncConfiguration.Configurations;

namespace TeraSyncV2.TeraSyncConfiguration;

public interface IConfigService<out T> : IDisposable where T : ITeraSyncConfiguration
{
    T Current { get; }
    string ConfigurationName { get; }
    string ConfigurationPath { get; }
    public event EventHandler? ConfigSave;
    void UpdateLastWriteTime();
}
