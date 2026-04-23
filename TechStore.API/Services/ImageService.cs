using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TechStore.API.Services;

public record ImageDto(string FileName, string Url, string SizeFormatted, DateTime UploadedAt);
public record UploadResult(List<ImageDto> Uploaded, List<string> Errors);

public interface IImageService
{
    Task<UploadResult> UploadAsync(IEnumerable<IFormFile> files);
    Task<List<ImageDto>> GetAllAsync();
    Task<bool> DeleteAsync(string fileName);
}

public class ImageService : IImageService
{
    private static readonly HashSet<string> _allowed =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly BlobContainerClient _container;
    private readonly string _cdnBase;

    public ImageService(BlobServiceClient blobServiceClient, IConfiguration config)
    {
        _container = blobServiceClient.GetBlobContainerClient("images");
        _cdnBase = config["AzureStorage:CdnBaseUrl"]!.TrimEnd('/');
    }

    public async Task<UploadResult> UploadAsync(IEnumerable<IFormFile> files)
    {
        var uploaded = new List<ImageDto>();
        var errors = new List<string>();

        await _container.CreateIfNotExistsAsync(PublicAccessType.None);

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_allowed.Contains(ext))
            {
                errors.Add($"'{file.FileName}': unsupported type (allowed: jpg, png, webp, gif).");
                continue;
            }
            if (file.Length > MaxBytes)
            {
                errors.Add($"'{file.FileName}': exceeds 5 MB limit.");
                continue;
            }

            var safeName = Path.GetFileNameWithoutExtension(file.FileName)
                               .ToLowerInvariant()
                               .Replace(" ", "-");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var fileName = $"{timestamp}-{safeName}{ext}";

            var blobClient = _container.GetBlobClient(fileName);
            var headers = new BlobHttpHeaders { ContentType = GetContentType(ext) };

            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers });

            uploaded.Add(new ImageDto(
                fileName,
                BuildCdnUrl(fileName),
                FormatSize(file.Length),
                DateTime.UtcNow
            ));
        }

        return new UploadResult(uploaded, errors);
    }

    public async Task<List<ImageDto>> GetAllAsync()
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.None);

        var result = new List<ImageDto>();

        await foreach (var blob in _container.GetBlobsAsync())
        {
            if (!_allowed.Contains(Path.GetExtension(blob.Name).ToLowerInvariant()))
                continue;

            result.Add(new ImageDto(
                blob.Name,
                BuildCdnUrl(blob.Name),
                FormatSize(blob.Properties.ContentLength ?? 0),
                blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow
            ));
        }

        return result.OrderByDescending(i => i.UploadedAt).ToList();
    }

    public async Task<bool> DeleteAsync(string fileName)
    {
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return false;

        var blobClient = _container.GetBlobClient(fileName);
        var response = await blobClient.DeleteIfExistsAsync();
        return response.Value;
    }

    // Permanent CDN URL — no expiry, served via Azure Front Door
    // e.g. https://hytelimages-e2fge0bsabapfnd7.z03.azurefd.net/images/abc123.jpg
    private string BuildCdnUrl(string fileName) => $"{_cdnBase}/images/{fileName}";

    private static string GetContentType(string ext) => ext switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}