using Microsoft.Extensions.Logging;
using TeraSyncV2.API.Dto.User;
using TeraSyncV2.PlayerData.Pairs;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.Services.ServerConfiguration;

namespace TeraSyncV2.PlayerData.Factories;

public class PairFactory
{
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TeraMediator _teraMediator;
    private readonly ServerConfigurationManager _serverConfigurationManager;

    public PairFactory(ILoggerFactory loggerFactory, PairHandlerFactory cachedPlayerFactory,
        TeraMediator teraMediator, ServerConfigurationManager serverConfigurationManager)
    {
        _loggerFactory = loggerFactory;
        _cachedPlayerFactory = cachedPlayerFactory;
        _teraMediator = teraMediator;
        _serverConfigurationManager = serverConfigurationManager;
    }

    public Pair Create(UserFullPairDto userPairDto)
    {
        return new Pair(_loggerFactory.CreateLogger<Pair>(), userPairDto, _cachedPlayerFactory, _teraMediator, _serverConfigurationManager);
    }

    public Pair Create(UserPairDto userPairDto)
    {
        return new Pair(_loggerFactory.CreateLogger<Pair>(), new(userPairDto.User, userPairDto.IndividualPairStatus, [], userPairDto.OwnPermissions, userPairDto.OtherPermissions),
            _cachedPlayerFactory, _teraMediator, _serverConfigurationManager);
    }
}