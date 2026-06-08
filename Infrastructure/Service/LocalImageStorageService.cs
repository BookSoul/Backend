using Application.Interface;
using Microsoft.AspNetCore.Hosting;

namespace Infrastructure.Service;

public class LocalImageStorageService : IImageStorageService
{
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
        Directory.CreateDirectory(_uploadRoot);

        var extension = Path.GetExtension(fileName);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadRoot, safeName);

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return $"/uploads/{safeName}";
    }
}
