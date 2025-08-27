using TeraSyncV2.API.Dto.Files;

namespace TeraSyncV2.WebAPI.Files.Models;

public class UploadFileTransfer : FileTransfer
{
    public UploadFileTransfer(UploadFileDto dto) : base(dto)
    {
    }

    public string LocalFile { get; set; } = string.Empty;
    public override long Total { get; set; }
}