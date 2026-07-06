using Application.Interface;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Service;

public class LocalImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };
    private const int MaxImageBytes = 5 * 1024 * 1024;
    private readonly string _uploadRoot;

    public LocalImageStorageService(IWebHostEnvironment env)
    {
        var webRoot = env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(env.ContentRootPath, "wwwroot");
        }

        _uploadRoot = Path.Combine(webRoot, "uploads");
    }

    public async Task<string> UploadAsync(string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        if (content.Length == 0)
        {
            throw new InvalidOperationException("File ảnh không hợp lệ.");
        }

        if (content.Length > MaxImageBytes)
        {
            throw new InvalidOperationException("Ảnh tải lên không được vượt quá 5MB.");
        }

        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ cho phép tải lên file ảnh.");
        }

        Directory.CreateDirectory(_uploadRoot);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Định dạng ảnh phải là jpg, png, webp hoặc gif.");
        }

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadRoot, safeName);

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return $"/uploads/{safeName}";
    }
}
