using FluentValidation;
using LeaveManagement.Application.DTOs.LeaveType;

namespace LeaveManagement.Application.Validators;

public class CreateLeaveTypeValidator : AbstractValidator<CreateLeaveTypeDto>
{
    public CreateLeaveTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Leave type name is required.")
            .MaximumLength(50).WithMessage("Leave type name cannot exceed 50 characters.");

        RuleFor(x => x.DefaultDays)
            .GreaterThan(0).WithMessage("Default leave days must be greater than 0.")
            .LessThanOrEqualTo(365).WithMessage("Default leave days cannot exceed 365 days.");
    }
}