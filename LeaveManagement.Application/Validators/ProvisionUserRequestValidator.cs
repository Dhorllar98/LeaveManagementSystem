using FluentValidation;
using LeaveManagement.Application.DTOs.User;
using LeaveManagement.Domain.Enums;

namespace LeaveManagement.Application.Validators;

public class ProvisionUserRequestValidator : AbstractValidator<ProvisionUserDto>
{
    public ProvisionUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .WithMessage("Full name must include both a first name and a last name.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
            .WithMessage("Email address is invalid.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role.");

        RuleFor(x => x.Designation)
            .NotEmpty().WithMessage("Designation is required.")
            .MaximumLength(100).WithMessage("Designation must not exceed 100 characters.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required.");

        RuleFor(x => x.ResetPasswordUrl)
            .NotEmpty().WithMessage("Reset password URL is required.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Reset password URL must be a valid absolute HTTP/HTTPS URL.");
    }
}