using FluentValidation;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Validates <see cref="CreateEventCommand"/>.</summary>
public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    /// <summary>Initializes validation rules.</summary>
    private static readonly string[] ValidEventTypes = ["Wedding", "Reception", "Birthday", "Corporate", "Outdoor", "Other"];

    public CreateEventCommandValidator()
    {
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

        RuleFor(x => x.WatchFolder)
            .NotEmpty().WithMessage("Watch folder path is required.")
            .MinimumLength(3).WithMessage("Watch folder path is too short.")
            .MaximumLength(512).WithMessage("Watch folder path must not exceed 512 characters.")
            .Must(p => !p.Contains("..")).WithMessage("Watch folder path must not contain path traversal sequences.");

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

        RuleFor(x => x.GalleryRecentCount)
            .InclusiveBetween(1, 1000).WithMessage("Gallery recent count must be between 1 and 1000.")
            .When(x => x.GalleryRecentCount.HasValue);

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
