using FluentValidation;
using TaskHub.Api.Dto;

namespace Task_hub.Application.Validators;

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must not exceed 50 characters")
            .Must(tags => tags == null || tags.All(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^[a-zA-Z0-9\-_]+$")))
            .WithMessage("Tags can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.DueDate)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("Due date must be in the future");
    }
}

public class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(x => x.Title)
            .MinimumLength(3).WithMessage("Title must be at least 3 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.Tags)
            .Must(tags => tags == null || tags.All(t => t.Length <= 50))
            .WithMessage("Each tag must not exceed 50 characters")
            .Must(tags => tags == null || tags.All(t => System.Text.RegularExpressions.Regex.IsMatch(t, @"^[a-zA-Z0-9\-_]+$")))
            .WithMessage("Tags can only contain letters, numbers, hyphens, and underscores")
            .When(x => x.Tags != null);

        RuleFor(x => x.DueDate)
            .Must(date => !date.HasValue || date.Value > DateTime.UtcNow)
            .WithMessage("Due date must be in the future")
            .When(x => x.DueDate.HasValue);
    }
}
