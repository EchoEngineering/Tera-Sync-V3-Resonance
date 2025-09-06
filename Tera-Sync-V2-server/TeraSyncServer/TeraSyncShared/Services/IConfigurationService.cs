using TeraSyncV2Shared.Utils.Configuration;

namespace TeraSyncV2Shared.Services;

public interface IConfigurationService<T> where T : class, ITeraConfiguration
{
    bool IsMain { get; }

    event EventHandler ConfigChangedEvent;

    T1 GetValue<T1>(string key);
    T1 GetValueOrDefault<T1>(string key, T1 defaultValue);
    string ToString();
}
