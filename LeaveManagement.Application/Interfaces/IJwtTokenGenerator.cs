using LeaveManagement.Domain.Entities;

namespace LeaveManagement.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}