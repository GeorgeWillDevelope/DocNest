using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("UploadFile")]
        public async Task<IActionResult> UploadFile(IFormFile files)
        {
            if (files == null || files.Length == 0)
                return BadRequest("File not selected");

            // Process the file here
            // For example, save it to the server or perform any required actions

            // You can replace this logic with your actual file processing logic
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", files.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await files.CopyToAsync(stream);
            }

            return Ok("File uploaded successfully");
        }
    }
}
