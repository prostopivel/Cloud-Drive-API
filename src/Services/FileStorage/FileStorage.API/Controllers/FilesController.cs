using FileStorage.API.DTOs;
using FileStorage.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStorage.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileStorageService fileStorageService,
            ILogger<FilesController> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file,
            [FromHeader] Guid userId)
        {
            var storedFile = await _fileStorageService.UploadFileAsync(file, userId);

            _logger.LogInformation("File uploaded successfully: {FileName} ({Size} bytes)",
                storedFile.FileName, storedFile.Size);

            return Ok(new FileUploadResponse
            {
                FileId = storedFile.Id,
                FileName = storedFile.OriginalName,
                Size = storedFile.Size,
                UploadedAt = storedFile.UploadedAt
            });
        }

        [HttpGet("download/{fileId:guid}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            var result = await _fileStorageService.DownloadFileAsync(fileId);

            _logger.LogInformation("File downloaded: {FileId}", fileId);

            return File(result.FileStream, result.ContentType, result.FileName);
        }

        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            await _fileStorageService.DeleteFileAsync(fileId);

            _logger.LogInformation("File deleted: {FileId}", fileId);
            return Ok(new { message = "File deleted successfully" });
        }

        [HttpGet("{fileId:guid}/info")]
        public async Task<IActionResult> GetFileInfo(Guid fileId)
        {
            var fileInfo = await _fileStorageService.GetFileInfoAsync(fileId);

            return Ok(new FileInfoResponse
            {
                FileId = fileInfo.Id,
                FileName = fileInfo.FileName,
                Size = fileInfo.Size,
                ContentType = fileInfo.ContentType,
                UploadedAt = fileInfo.UploadedAt
            });
        }
    }
}
