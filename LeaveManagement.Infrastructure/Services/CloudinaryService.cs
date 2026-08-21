using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LeaveManagement.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace LeaveManagement.Infrastructure.Services;

public class CloudinaryService : IPhotoService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(Cloudinary cloudinary)
    {
        _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
    }

    public async Task<string> UploadImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new Domain.Exceptions.ValidationException("Invalid file provided.");
        }

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "organization-logos",
            Transformation = new Transformation().Width(500).Height(500).Crop("limit")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl.ToString();
    }
}