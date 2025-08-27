using MessagePack;
using TeraSyncV2.API.Data.Enum;

namespace TeraSyncV2.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record BulkPermissionsDto(Dictionary<string, UserPermissions> AffectedUsers, Dictionary<string, GroupUserPreferredPermissions> AffectedGroups);
