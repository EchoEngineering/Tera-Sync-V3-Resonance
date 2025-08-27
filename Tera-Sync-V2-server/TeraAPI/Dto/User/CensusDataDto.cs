using MessagePack;

namespace TeraSyncV2.API.Dto.User;

[MessagePackObject(keyAsPropertyName: true)]
public record CensusDataDto(ushort WorldId, short RaceId, short TribeId, short Gender);