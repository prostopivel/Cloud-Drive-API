using FileStorage.Core.Entities;
using Microsoft.AspNetCore.Http;

namespace FileStorage.Core.Interfaces.Services
{
    public interface IFileStorageService
    {
        Task<StoredFile> UploadFileAsync(IFormFile file, Guid userId,
            CancellationToken token = default);
        Task<FileDownloadResult> DownloadFileAsync(Guid fileId, CancellationToken token = default);
        Task DeleteFileAsync(Guid fileId, CancellationToken token = default);
        Task<StoredFile> GetFileInfoAsync(Guid fileId, CancellationToken token = default);
    }
}
