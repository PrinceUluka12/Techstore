using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SkiaSharp;

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

    private const long MaxBytes = 10 * 1024 * 1024; // 10 MB as uploaded, before shrinking

    // Uploads are resized and saved as WebP: the full image for the product page, and a small
    // copy with the same name in thumbs/ for product lists, carts and admin tables
    private const int FullSize = 1200, FullQuality = 80;
    private const int ThumbSize = 480, ThumbQuality = 75;
    private const string ThumbFolder = "thumbs/";

    // File names are timestamped and never reused, so browsers can keep an image for a year
    private const string CacheControl = "public, max-age=31536000, immutable";

    private readonly BlobContainerClient _container;
    private readonly string _publicBase;

    public ImageService(BlobServiceClient blobServiceClient, IConfiguration config)
    {
        _container = blobServiceClient.GetBlobContainerClient("images");

        // Images are read straight from the storage container. To put a CDN in front later,
        // set AzureStorage:CdnBaseUrl to its address (serving the container at /images).
        var cdn = config["AzureStorage:CdnBaseUrl"];
        _publicBase = string.IsNullOrWhiteSpace(cdn)
            ? _container.Uri.ToString().TrimEnd('/')
            : $"{cdn.TrimEnd('/')}/images";
    }

    public async Task<UploadResult> UploadAsync(IEnumerable<IFormFile> files)
    {
        var uploaded = new List<ImageDto>();
        var errors = new List<string>();

        await EnsureContainerAsync();

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
                errors.Add($"'{file.FileName}': exceeds 10 MB limit.");
                continue;
            }

            var baseName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{SafeName(file.FileName)}";
            await using var stream = file.OpenReadStream();

            // GIFs are kept as uploaded so animations still play (no thumbnail)
            if (ext == ".gif")
            {
                var gifName = baseName + ".gif";
                await UploadBlobAsync(gifName, stream, "image/gif");
                uploaded.Add(new ImageDto(gifName, BuildUrl(gifName), FormatSize(file.Length), DateTime.UtcNow));
                continue;
            }

            using var source = Decode(stream);
            if (source == null)
            {
                errors.Add($"'{file.FileName}': couldn't be read as an image.");
                continue;
            }

            var fileName = baseName + ".webp";
            using var full  = Encode(source, FullSize, FullQuality);
            using var thumb = Encode(source, ThumbSize, ThumbQuality);
            await UploadBlobAsync(fileName, full.AsStream(), "image/webp");
            await UploadBlobAsync(ThumbFolder + fileName, thumb.AsStream(), "image/webp");

            uploaded.Add(new ImageDto(fileName, BuildUrl(fileName), FormatSize(full.Size), DateTime.UtcNow));
        }

        return new UploadResult(uploaded, errors);
    }

    public async Task<List<ImageDto>> GetAllAsync()
    {
        await EnsureContainerAsync();

        var result = new List<ImageDto>();

        await foreach (var blob in _container.GetBlobsAsync())
        {
            // Thumbnails live in thumbs/ and go with their image
            if (blob.Name.Contains('/') || !_allowed.Contains(Path.GetExtension(blob.Name).ToLowerInvariant()))
                continue;

            result.Add(new ImageDto(
                blob.Name,
                BuildUrl(blob.Name),
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

        var response = await _container.GetBlobClient(fileName).DeleteIfExistsAsync();
        await _container.GetBlobClient(ThumbFolder + fileName).DeleteIfExistsAsync();
        return response.Value;
    }

    // Browsers load images straight from the container, so a new one allows anonymous reads
    // of its blobs (the storage account must allow that too). An existing one is left as it is.
    private Task EnsureContainerAsync() => _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

    private Task UploadBlobAsync(string name, Stream content, string contentType) =>
        _container.GetBlobClient(name).UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = CacheControl },
        });

    // e.g. https://hytelstorage.blob.core.windows.net/images/20261031120000000-galaxy-a55.webp
    private string BuildUrl(string fileName) => $"{_publicBase}/{fileName}";

    // Decodes the image and turns it the right way up: phone photos are often stored sideways
    // with an EXIF orientation flag, and that flag is lost when the image is re-encoded
    private static SKBitmap? Decode(Stream stream)
    {
        using var data = SKData.Create(stream);
        using var codec = data == null ? null : SKCodec.Create(data);
        if (codec == null) return null;
        var bitmap = SKBitmap.Decode(codec);
        return bitmap == null ? null : Orient(bitmap, codec.EncodedOrigin);
    }

    private static SKBitmap Orient(SKBitmap src, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft) return src;

        // Where each stored pixel (x, y) belongs on screen:
        // x' = a·x + b·y + c,  y' = d·x + e·y + f
        float w = src.Width, h = src.Height;
        var m = origin switch
        {
            SKEncodedOrigin.TopRight    => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),  // mirrored
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), // upside down
            SKEncodedOrigin.BottomLeft  => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),  // mirrored upside down
            SKEncodedOrigin.LeftTop     => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),   // mirrored and sideways
            SKEncodedOrigin.RightTop    => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),  // needs a quarter turn clockwise
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), // mirrored and sideways
            SKEncodedOrigin.LeftBottom  => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),  // needs a quarter turn anticlockwise
            _ => SKMatrix.Identity,
        };
        var sideways = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                              or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var dst = new SKBitmap(new SKImageInfo(sideways ? src.Height : src.Width, sideways ? src.Width : src.Height,
                                               src.ColorType, src.AlphaType));
        using (var canvas = new SKCanvas(dst))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.SetMatrix(m);
            canvas.DrawBitmap(src, 0, 0);
        }
        src.Dispose();
        return dst;
    }

    // Shrinks to fit a size×size box (never enlarges) and saves as WebP
    private static SKData Encode(SKBitmap source, int size, int quality)
    {
        var scale = Math.Min(1f, (float)size / Math.Max(source.Width, source.Height));
        int width  = Math.Max(1, (int)Math.Round(source.Width * scale));
        int height = Math.Max(1, (int)Math.Round(source.Height * scale));
        var sampling = new SKSamplingOptions(SKFilterMode.Linear);

        // Halve while it's more than twice too big, then do the last step: a single big
        // jump with linear sampling skips pixels and looks jagged
        var current = source;
        while (current.Width >= width * 2 && current.Height >= height * 2)
        {
            var half = current.Resize(new SKImageInfo(current.Width / 2, current.Height / 2, source.ColorType, source.AlphaType), sampling);
            if (current != source) current.Dispose();
            current = half;
        }
        if (current.Width != width || current.Height != height)
        {
            var resized = current.Resize(new SKImageInfo(width, height, source.ColorType, source.AlphaType), sampling);
            if (current != source) current.Dispose();
            current = resized;
        }

        using var image = SKImage.FromBitmap(current);
        var encoded = image.Encode(SKEncodedImageFormat.Webp, quality);
        if (current != source) current.Dispose();
        return encoded;
    }

    // "My Photo (1).JPG" → "my-photo-1"
    private static string SafeName(string fileName)
    {
        var name = Regex.Replace(Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (name.Length > 60) name = name[..60].TrimEnd('-');
        return name.Length == 0 ? "image" : name;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB",
    };
}
