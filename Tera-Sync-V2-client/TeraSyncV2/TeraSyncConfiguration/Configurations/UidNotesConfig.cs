using TeraSyncV2.TeraSyncConfiguration.Models;

namespace TeraSyncV2.TeraSyncConfiguration.Configurations;

public class UidNotesConfig : ITeraSyncConfiguration
{
    public Dictionary<string, ServerNotesStorage> ServerNotes { get; set; } = new(StringComparer.Ordinal);
    public int Version { get; set; } = 0;
}
