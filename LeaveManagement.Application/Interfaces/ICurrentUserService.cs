using System;

namespace LeaveManagement.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}