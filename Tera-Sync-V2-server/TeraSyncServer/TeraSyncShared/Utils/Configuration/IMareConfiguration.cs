namespace TeraSyncV2Shared.Utils.Configuration;

public interface ITeraConfiguration
{
    T GetValueOrDefault<T>(string key, T defaultValue);
    T GetValue<T>(string key);
    string SerializeValue(string key, string defaultValue);
}
