using MessagePack;

namespace TeraSyncV2.API.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record UserData(string UID, string? Alias = null)
{
    [IgnoreMember]
    public string AliasOrUID => string.IsNullOrWhiteSpace(Alias) ? UID : Alias;
}