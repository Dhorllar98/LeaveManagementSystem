using FluentValidation;
using LeaveManagement.Application.DTOs.Auth;

namespace LeaveManagement.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .WithMessage("Full name must include both a first name and a last name.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]|[^a-zA-Z0-9]").WithMessage("Password must contain at least one number or special character.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Invalid user role.");
    }
}