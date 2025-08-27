using MessagePack;
using TeraSyncV2.API.Data;

namespace TeraSyncV2.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record OnlineUserIdentDto(UserData User, string Ident) : UserDto(User);