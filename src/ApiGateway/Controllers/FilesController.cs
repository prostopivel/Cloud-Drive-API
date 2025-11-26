using ApiGateway.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IUserIdValidator _userIdValidator;
        private readonly IFilesService _filesService;

        public FilesController(IUserIdValidator userIdValidator,
            IFilesService filesService)
        {
            _userIdValidator = userIdValidator;
            _filesService = filesService;
        }

        [HttpGet("download/{fileId:guid}")]
        public async Task<IActionResult> DownloadFile(Guid fileId)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var id = _userIdValidator.Validate(userId);

            await _filesService.DownloadFileAsync(fileId, id);

            return Redirect($"/api/files/download/{fileId}");
        }

        [HttpDelete("{fileId:guid}")]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            var userId = HttpContext.Items["UserId"]?.ToString();
            var id = _userIdValidator.Validate(userId);

            await _filesService.DeleteFileAsync(fileId, id);

            return Ok(new { message = "File deleted successfully" });
        }
    }
}
