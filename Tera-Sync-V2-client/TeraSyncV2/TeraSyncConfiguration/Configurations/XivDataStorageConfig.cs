using System.Collections.Concurrent;

namespace TeraSyncV2.TeraSyncConfiguration.Configurations;

public class XivDataStorageConfig : ITeraSyncConfiguration
{
    public ConcurrentDictionary<string, long> TriangleDictionary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public ConcurrentDictionary<string, Dictionary<string, List<ushort>>> BonesDictionary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Version { get; set; } = 0;
}