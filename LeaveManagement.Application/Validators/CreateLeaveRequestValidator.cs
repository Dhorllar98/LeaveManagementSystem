using FluentValidation;
using LeaveManagement.Application.Common.Helpers;
using LeaveManagement.Application.DTOs.LeaveRequest;

namespace LeaveManagement.Application.Validators;

public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveTypeId)
            .NotEmpty().WithMessage("Leave type is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("End date must be on or after start date.");

        RuleFor(x => x)
            .Must(x => DateHelper.CalculateBusinessDays(x.StartDate, x.EndDate) > 0)
            .WithMessage("The selected date range must include at least one working day (Monday to Friday).");
    }
}