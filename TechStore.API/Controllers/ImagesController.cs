using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.API.Services;

namespace TechStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ImagesController : ControllerBase
{
    private readonly IImageService _images;
    public ImagesController(IImageService images) => _images = images;

    /// <summary>Upload one or more images — stored in Azure Blob Storage (Admin only)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB total request
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files provided." });

        var result = await _images.UploadAsync(files);
        return Ok(new { uploaded = result.Uploaded, errors = result.Errors });
    }

    /// <summary>List all uploaded images from Blob Storage (Admin only)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _images.GetAllAsync());

    /// <summary>Delete an image from Blob Storage by filename (Admin only)</summary>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> Delete(string fileName)
    {
        var deleted = await _images.DeleteAsync(fileName);
        return deleted
            ? Ok(new { message = "Image deleted." })
            : NotFound(new { message = "Image not found." });
    }
}