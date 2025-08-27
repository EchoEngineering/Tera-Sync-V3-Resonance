using MessagePack;
using TeraSyncV2.API.Data;
using TeraSyncV2.API.Data.Enum;

namespace TeraSyncV2.API.Dto.Group;

[MessagePackObject(keyAsPropertyName: true)]
public record GroupPairUserInfoDto(GroupData Group, UserData User, GroupPairUserInfo GroupUserInfo) : GroupPairDto(Group, User);