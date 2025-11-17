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
            [FromHeader] Guid? userId = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            try
            {
                var storedFile = await _fileStorageService.UploadFileAsync(file, userId);

                _logger.LogInformation("File uploaded successfully: {FileName} ({Size} bytes)",
                    storedFile.FileName, storedFile.Size);

                return Ok(new FileUploadResponse
                {
                    FileId = storedFile.Id,
                    FileName = storedFile.FileName,
                    Size = storedFile.Size,
                    UploadedAt = storedFile.UploadedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return StatusCode(500, "Error uploading file");
            }
        }

        [HttpGet("download/{fileId:guid}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            try
            {
                var result = await _fileStorageService.DownloadFileAsync(fileId);

                _logger.LogInformation("File downloaded: {FileId}", fileId);

                return File(result.FileStream, result.ContentType, result.FileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound($"File with id {fileId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId}", fileId);
                return StatusCode(500, "Error downloading file");
            }
        }

        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            try
            {
                var deleted = await _fileStorageService.DeleteFileAsync(fileId);

                if (!deleted)
                {
                    return NotFound($"File with id {fileId} not found");
                }

                _logger.LogInformation("File deleted: {FileId}", fileId);
                return Ok(new { message = "File deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId}", fileId);
                return StatusCode(500, "Error deleting file");
            }
        }

        [HttpGet("{fileId:guid}/info")]
        public async Task<IActionResult> GetFileInfo(Guid fileId)
        {
            try
            {
                var fileInfo = await _fileStorageService.GetFileInfoAsync(fileId);

                if (fileInfo == null)
                {
                    return NotFound($"File with id {fileId} not found");
                }

                return Ok(new FileInfoResponse
                {
                    FileId = fileInfo.Id,
                    FileName = fileInfo.FileName,
                    Size = fileInfo.Size,
                    ContentType = fileInfo.ContentType,
                    UploadedAt = fileInfo.UploadedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info {FileId}", fileId);
                return StatusCode(500, "Error getting file info");
            }
        }
    }
}
