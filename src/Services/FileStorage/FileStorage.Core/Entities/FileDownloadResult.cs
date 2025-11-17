namespace FileStorage.Core.Entities
{
    public record FileDownloadResult(Stream FileStream,
        string FileName,
        string ContentType);
}
