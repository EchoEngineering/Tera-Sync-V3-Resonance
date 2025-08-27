using TeraSyncV2.TeraSyncConfiguration.Models;

namespace TeraSyncV2.TeraSyncConfiguration.Configurations;

public class ServerTagConfig : ITeraSyncConfiguration
{
    public Dictionary<string, ServerTagStorage> ServerTagStorage { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Version { get; set; } = 0;
}