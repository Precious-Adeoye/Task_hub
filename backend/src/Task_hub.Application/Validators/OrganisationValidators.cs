using FluentValidation;
using TaskHub.Api.Dto;

namespace Task_hub.Application.Validators;

public class CreateOrganisationRequestValidator : AbstractValidator<CreateOrganisationRequest>
{
    public CreateOrganisationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Organisation name is required")
            .MinimumLength(3).WithMessage("Organisation name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Organisation name must not exceed 100 characters")
            .Matches("^[a-zA-Z0-9\\s\\-_]+$").WithMessage("Organisation name can only contain letters, numbers, spaces, hyphens, and underscores");
    }
}

public class AddMemberRequestValidator : AbstractValidator<AddMemberRequest>
{
    public AddMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(r => r == "Member" || r == "Admin")
            .WithMessage("Role must be either 'Member' or 'Admin'");
    }
}
