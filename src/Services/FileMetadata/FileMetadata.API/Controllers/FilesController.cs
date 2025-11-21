using FileMetadata.API.DTOs;
using FileMetadata.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileMetadata.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileMetadataService _fileMetadataService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileMetadataService fileMetadataService, ILogger<FilesController> logger)
        {
            _fileMetadataService = fileMetadataService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserFiles([FromHeader] Guid userId)
        {
            try
            {
                var files = await _fileMetadataService.GetUserFilesAsync(userId);
                var response = files.Select(f => new FileMetadataResponse
                {
                    FileId = f.Id,
                    FileName = f.OriginalName,
                    Size = f.Size,
                    ContentType = f.ContentType,
                    UploadedAt = f.UploadedAt,
                    LastAccessedAt = f.LastAccessedAt
                });

                _logger.LogInformation("Retrieved {Count} files for user {UserId}", files.Count(), userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for user {UserId}", userId);
                return StatusCode(500, "Error retrieving files");
            }
        }

        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> GetFileMetadata(Guid fileId, [FromHeader] Guid userId)
        {
            try
            {
                var fileMetadata = await _fileMetadataService.GetFileMetadataAsync(fileId);
                if (fileMetadata == null)
                    return NotFound($"File with id {fileId} not found");

                if (fileMetadata.UserId != userId)
                    return Forbid("File does not belong to user");

                var response = new FileMetadataResponse
                {
                    FileId = fileMetadata.Id,
                    FileName = fileMetadata.OriginalName,
                    Size = fileMetadata.Size,
                    ContentType = fileMetadata.ContentType,
                    UploadedAt = fileMetadata.UploadedAt,
                    LastAccessedAt = fileMetadata.LastAccessedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file metadata {FileId}", fileId);
                return StatusCode(500, "Error retrieving file metadata");
            }
        }

        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFileMetadata(Guid fileId, [FromHeader] Guid userId)
        {
            try
            {
                var belongsToUser = await _fileMetadataService.BelongsToUserAsync(fileId, userId);
                if (!belongsToUser)
                    return Forbid("File does not belong to user");

                var deleted = await _fileMetadataService.DeleteFileMetadataAsync(fileId);
                if (!deleted)
                    return NotFound($"File with id {fileId} not found");

                _logger.LogInformation("File metadata deleted: {FileId} by user {UserId}", fileId, userId);
                return Ok(new { message = "File metadata deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file metadata {FileId}", fileId);
                return StatusCode(500, "Error deleting file metadata");
            }
        }

        [HttpPost("{fileId:guid}/validate-ownership")]
        public async Task<IActionResult> ValidateOwnership(Guid fileId, [FromHeader] Guid userId)
        {
            try
            {
                var belongsToUser = await _fileMetadataService.BelongsToUserAsync(fileId, userId);

                _logger.LogDebug("Ownership validation: File {FileId}, User {UserId}, Result: {Result}",
                    fileId, userId, belongsToUser);

                return Ok(new { belongsToUser });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ownership for file {FileId}", fileId);
                return StatusCode(500, "Error validating ownership");
            }
        }
    }
}
