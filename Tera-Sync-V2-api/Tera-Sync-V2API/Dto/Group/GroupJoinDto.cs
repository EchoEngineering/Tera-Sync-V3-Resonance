using MessagePack;
using TeraSyncV2.API.Data;
using TeraSyncV2.API.Data.Enum;

namespace TeraSyncV2.API.Dto.Group;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupPasswordDto(GroupData Group, string Password) : GroupDto(Group);

[MessagePackObject(keyAsPropertyName: true)]
public record GroupJoinDto(GroupData Group, string Password, GroupUserPreferredPermissions GroupUserPreferredPermissions) : GroupPasswordDto(Group, Password);