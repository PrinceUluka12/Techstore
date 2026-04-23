using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechStore.API.Controllers;

public record ImageDto(
    string FileName,
    string Url,
    long SizeBytes,
    string SizeFormatted,
    DateTime UploadedAt
);

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ImagesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public ImagesController(IWebHostEnvironment env, IConfiguration config)
    {
        _env = env;
        _config = config;
    }

    private string UploadsFolder
    {
        get
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(webRoot, "uploads", "images");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    private string GetFileUrl(string fileName)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return $"{baseUrl}/uploads/images/{fileName}";
    }

    /// <summary>Upload one or more images (Admin only)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB total
    public async Task<IActionResult> Upload(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files provided." });

        var results = new List<ImageDto>();
        var errors  = new List<string>();

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext))
            {
                errors.Add($"{file.FileName}: unsupported type. Allowed: jpg, jpeg, png, webp, gif.");
                continue;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                errors.Add($"{file.FileName}: exceeds 5 MB limit.");
                continue;
            }

            // unique filename: timestamp + original name (sanitised)
            var safeName   = Path.GetFileNameWithoutExtension(file.FileName)
                                 .Replace(" ", "-")
                                 .ToLowerInvariant();
            var fileName   = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{safeName}{ext}";
            var filePath   = Path.Combine(UploadsFolder, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            results.Add(new ImageDto(
                fileName,
                GetFileUrl(fileName),
                file.Length,
                FormatSize(file.Length),
                DateTime.UtcNow
            ));
        }

        return Ok(new { uploaded = results, errors });
    }

    /// <summary>List all uploaded images (Admin only)</summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        var files = Directory.GetFiles(UploadsFolder)
            .Select(f =>
            {
                var info = new FileInfo(f);
                return new ImageDto(
                    info.Name,
                    GetFileUrl(info.Name),
                    info.Length,
                    FormatSize(info.Length),
                    info.CreationTimeUtc
                );
            })
            .OrderByDescending(f => f.UploadedAt)
            .ToList();

        return Ok(files);
    }

    /// <summary>Delete an image by filename (Admin only)</summary>
    [HttpDelete("{fileName}")]
    public IActionResult Delete(string fileName)
    {
        // prevent path traversal
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return BadRequest(new { message = "Invalid filename." });

        var filePath = Path.Combine(UploadsFolder, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Image not found." });

        System.IO.File.Delete(filePath);
        return NoContent();
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024               => $"{bytes} B",
        < 1024 * 1024        => $"{bytes / 1024.0:F1} KB",
        _                    => $"{bytes / (1024.0 * 1024):F1} MB"
    };
}