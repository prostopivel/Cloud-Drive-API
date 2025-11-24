using FileMetadata.API.DTOs;
using FileMetadata.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.Exceptions;

namespace FileMetadata.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileMetadataService _fileMetadataService;

        public FilesController(IFileMetadataService fileMetadataService)
        {
            _fileMetadataService = fileMetadataService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserFiles([FromHeader] Guid userId,
            CancellationToken token = default)
        {
            var files = await _fileMetadataService.GetUserFilesAsync(userId,
                token: token);
            var response = files.Select(f => new FileMetadataResponse
            {
                FileId = f.Id,
                FileName = f.OriginalName,
                Size = f.Size,
                ContentType = f.ContentType,
                UploadedAt = f.UploadedAt,
                LastAccessedAt = f.LastAccessedAt
            });

            return Ok(response);
        }

        [HttpGet("{fileId:guid}")]
        public async Task<IActionResult> GetFileMetadata(Guid fileId,
            [FromHeader] Guid userId,
            CancellationToken token = default)
        {
            var fileMetadata = await _fileMetadataService.GetFileMetadataAsync(fileId,
                userId,
                token: token);

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

        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFileMetadata(Guid fileId,
            [FromHeader] Guid userId,
            CancellationToken token = default)
        {
            await _fileMetadataService.DeleteFileMetadataAsync(fileId,
                userId,
                token: token);

            return Ok(new { message = "File metadata deleted successfully" });
        }

        [HttpPost("{fileId:guid}/validate-ownership")]
        public async Task<IActionResult> ValidateOwnership(Guid fileId,
            [FromHeader] Guid userId,
            CancellationToken token = default)
        {
            try
            {
                await _fileMetadataService.BelongsToUserAsync(fileId,
                    userId,
                token: token);
            }
            catch (ForbidException)
            {
                return Ok(new { belongsToUser = false });
            }

            return Ok(new { belongsToUser = true });
        }
    }
}
