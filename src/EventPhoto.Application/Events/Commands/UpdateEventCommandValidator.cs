using FluentValidation;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Validates <see cref="UpdateEventCommand"/>.</summary>
public sealed class UpdateEventCommandValidator : AbstractValidator<UpdateEventCommand>
{
    private static readonly string[] ValidEventTypes = ["Wedding", "Reception", "Birthday", "Corporate", "Outdoor", "Other"];

    /// <summary>Initializes validation rules.</summary>
    public UpdateEventCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("EventId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Event name is required.")
            .MinimumLength(2).WithMessage("Event name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Event name must not exceed 200 characters.");

        RuleFor(x => x.EventType)
            .NotEmpty().WithMessage("Event type is required.")
            .Must(t => ValidEventTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("EventType must be one of: Wedding, Reception, Birthday, Corporate, Outdoor, Other.");

        RuleFor(x => x.EventDate)
            .NotEmpty().WithMessage("Event date is required.")
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)))
            .WithMessage("Event date cannot be more than 10 years in the past.")
            .Must(d => d <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(5)))
            .WithMessage("Event date cannot be more than 5 years in the future.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.VenueName)
            .MinimumLength(2).WithMessage("Venue name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Venue name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.VenueName));

        RuleFor(x => x.ClientName)
            .MinimumLength(2).WithMessage("Client name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Client name must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ClientName));
    }
}
