using FluentValidation;
using LeaveManagement.Application.DTOs.Auth; 

namespace LeaveManagement.Application.Validators;

public class RegisterOrganizationRequestValidator : AbstractValidator<RegisterOrganizationDto> 
{
    public RegisterOrganizationRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .Length(2, 100).WithMessage("Company name must be between 2 and 100 characters.");

        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithMessage("Admin full name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name) && name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .WithMessage("Admin full name must include both a first name and a last name.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
            .WithMessage("Email address is invalid.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[1-9]\d{7,14}$")
            .WithMessage("Please enter a valid phone number (e.g., +2348012345678).");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]|[^a-zA-Z0-9]").WithMessage("Password must contain at least one number or special character.");
    }
}