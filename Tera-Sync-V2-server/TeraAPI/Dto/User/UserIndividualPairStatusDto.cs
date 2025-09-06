using MessagePack;
using TeraSyncV2.API.Data;
using TeraSyncV2.API.Data.Enum;

namespace TeraSyncV2.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record UserIndividualPairStatusDto(UserData User, IndividualPairStatus IndividualPairStatus) : UserDto(User);