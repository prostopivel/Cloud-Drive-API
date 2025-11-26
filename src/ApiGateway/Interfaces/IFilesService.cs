using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Interfaces
{
    public interface IFilesService
    {
        Task DownloadFileAsync(Guid fileId, Guid userId);
        Task DeleteFileAsync(Guid fileId, Guid userId);
    }
}
