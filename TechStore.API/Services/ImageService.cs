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

    private readonly string _storagePath;

    public ImageService(IWebHostEnvironment env)
    {
        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _storagePath = Path.Combine(webRoot, "uploads", "images");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<UploadResult> UploadAsync(IEnumerable<IFormFile> files)
    {
        var uploaded = new List<ImageDto>();
        var errors   = new List<string>();

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

            // Timestamp prefix + sanitised original name, e.g. 20260422190937747-product-name.jpg
            var safeName = Path.GetFileNameWithoutExtension(file.FileName)
                              .ToLowerInvariant()
                              .Replace(" ", "-");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var fileName  = $"{timestamp}-{safeName}{ext}";
            var fullPath  = Path.Combine(_storagePath, fileName);

            await using var stream = File.Create(fullPath);
            await file.CopyToAsync(stream);

            uploaded.Add(new ImageDto(fileName, BuildUrl(fileName), FormatSize(file.Length), DateTime.UtcNow));
        }

        return new UploadResult(uploaded, errors);
    }

    public Task<List<ImageDto>> GetAllAsync()
    {
        var result = Directory.EnumerateFiles(_storagePath)
            .Where(f => _allowed.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Select(f =>
            {
                var info     = new FileInfo(f);
                var fileName = info.Name;
                return new ImageDto(fileName, BuildUrl(fileName), FormatSize(info.Length), info.LastWriteTimeUtc);
            })
            .ToList();

        return Task.FromResult(result);
    }

    public Task<bool> DeleteAsync(string fileName)
    {
        // Prevent path traversal
        if (fileName.Contains('/') || fileName.Contains('\\') || fileName.Contains(".."))
            return Task.FromResult(false);

        var fullPath = Path.Combine(_storagePath, fileName);
        if (!File.Exists(fullPath)) return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private static string BuildUrl(string fileName) => $"/uploads/images/{fileName}";

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024        => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _             => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}
