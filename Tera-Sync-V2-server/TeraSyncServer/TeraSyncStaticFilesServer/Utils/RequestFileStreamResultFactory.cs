using TeraSyncV2Shared.Metrics;
using TeraSyncV2Shared.Services;
using TeraSyncV2Shared.Utils.Configuration;
using TeraSyncV2StaticFilesServer.Services;

namespace TeraSyncV2StaticFilesServer.Utils;

public class RequestFileStreamResultFactory
{
    private readonly TeraMetrics _metrics;
    private readonly RequestQueueService _requestQueueService;
    private readonly IConfigurationService<StaticFilesServerConfiguration> _configurationService;

    public RequestFileStreamResultFactory(TeraMetrics metrics, RequestQueueService requestQueueService, IConfigurationService<StaticFilesServerConfiguration> configurationService)
    {
        _metrics = metrics;
        _requestQueueService = requestQueueService;
        _configurationService = configurationService;
    }

    public RequestFileStreamResult Create(Guid requestId, Stream stream)
    {
        return new RequestFileStreamResult(requestId, _requestQueueService,
            _metrics, stream, "application/octet-stream");
    }
}