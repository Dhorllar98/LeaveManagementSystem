using FluentValidation;
using LeaveManagement.Application.DTOs.User;

namespace LeaveManagement.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .Must(name => name == null || (!string.IsNullOrWhiteSpace(name) && name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2))
            .WithMessage("Full name must include both a first name and a last name.")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .Matches(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")
            .WithMessage("Email address format is invalid.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.LeaveBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Leave balance cannot be negative.")
            .When(x => x.LeaveBalance.HasValue);

        RuleFor(x => x.Designation)
            .MaximumLength(100)
            .WithMessage("Designation cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Designation));
    }
}