using Microsoft.Extensions.Logging;
using TeraSyncV2.FileCache;
using TeraSyncV2.Services.Mediator;
using TeraSyncV2.WebAPI.Files;

namespace TeraSyncV2.PlayerData.Factories;

public class FileDownloadManagerFactory
{
    private readonly FileCacheManager _fileCacheManager;
    private readonly FileCompactor _fileCompactor;
    private readonly FileTransferOrchestrator _fileTransferOrchestrator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TeraMediator _teraMediator;

    public FileDownloadManagerFactory(ILoggerFactory loggerFactory, TeraMediator teraMediator, FileTransferOrchestrator fileTransferOrchestrator,
        FileCacheManager fileCacheManager, FileCompactor fileCompactor)
    {
        _loggerFactory = loggerFactory;
        _teraMediator = teraMediator;
        _fileTransferOrchestrator = fileTransferOrchestrator;
        _fileCacheManager = fileCacheManager;
        _fileCompactor = fileCompactor;
    }

    public FileDownloadManager Create()
    {
        return new FileDownloadManager(_loggerFactory.CreateLogger<FileDownloadManager>(), _teraMediator, _fileTransferOrchestrator, _fileCacheManager, _fileCompactor);
    }
}