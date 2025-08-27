using MessagePack;
using TeraSyncV2.API.Data;
using TeraSyncV2.API.Data.Enum;

namespace TeraSyncV2.API.Dto.Group;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupPermissionDto(GroupData Group, GroupPermissions Permissions) : GroupDto(Group);