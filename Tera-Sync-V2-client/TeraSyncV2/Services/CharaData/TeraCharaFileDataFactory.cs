using TeraSyncV2.API.Data;
using TeraSyncV2.FileCache;
using TeraSyncV2.Services.CharaData.Models;

namespace TeraSyncV2.Services.CharaData;

public sealed class TeraCharaFileDataFactory
{
    private readonly FileCacheManager _fileCacheManager;

    public TeraCharaFileDataFactory(FileCacheManager fileCacheManager)
    {
        _fileCacheManager = fileCacheManager;
    }

    public TeraCharaFileData Create(string description, CharacterData characterCacheDto)
    {
        return new TeraCharaFileData(_fileCacheManager, description, characterCacheDto);
    }
}