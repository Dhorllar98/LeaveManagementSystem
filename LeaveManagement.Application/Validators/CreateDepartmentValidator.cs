using FluentValidation;
using LeaveManagement.Application.DTOs.Department;

namespace LeaveManagement.Application.Validators;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentDto>
{
    public CreateDepartmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required.")
            .Length(2, 100).WithMessage("Department name must be between 2 and 100 characters.");
    }
}