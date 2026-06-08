namespace Application.Interface;

public interface IImageStorageService
{
    Task<string> UploadAsync(string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default);
}

public record ImageUploadPayload(string FileName, string ContentType, byte[] Content);
