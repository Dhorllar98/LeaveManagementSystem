using Microsoft.AspNetCore.Http;

namespace LeaveManagement.Application.Interfaces;

public interface IPhotoService
{
    Task<string> UploadImageAsync(IFormFile file, CancellationToken cancellationToken = default);
}