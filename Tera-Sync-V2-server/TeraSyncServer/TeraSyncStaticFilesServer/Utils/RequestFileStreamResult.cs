using TeraSyncV2Shared.Metrics;
using TeraSyncV2StaticFilesServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace TeraSyncV2StaticFilesServer.Utils;

public class RequestFileStreamResult : FileStreamResult
{
    private readonly Guid _requestId;
    private readonly RequestQueueService _requestQueueService;
    private readonly TeraMetrics _teraMetrics;

    public RequestFileStreamResult(Guid requestId, RequestQueueService requestQueueService, TeraMetrics teraMetrics,
        Stream fileStream, string contentType) : base(fileStream, contentType)
    {
        _requestId = requestId;
        _requestQueueService = requestQueueService;
        _teraMetrics = teraMetrics;
        _teraMetrics.IncGauge(MetricsAPI.GaugeCurrentDownloads);
    }

    public override void ExecuteResult(ActionContext context)
    {
        try
        {
            base.ExecuteResult(context);
        }
        catch
        {
            throw;
        }
        finally
        {
            _requestQueueService.FinishRequest(_requestId);

            _teraMetrics.DecGauge(MetricsAPI.GaugeCurrentDownloads);
            FileStream?.Dispose();
        }
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        try
        {
            await base.ExecuteResultAsync(context).ConfigureAwait(false);
        }
        catch
        {
            throw;
        }
        finally
        {
            _requestQueueService.FinishRequest(_requestId);
            _teraMetrics.DecGauge(MetricsAPI.GaugeCurrentDownloads);
            FileStream?.Dispose();
        }
    }
}