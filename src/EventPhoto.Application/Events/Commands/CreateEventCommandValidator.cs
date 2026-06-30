using FluentValidation;

namespace EventPhoto.Application.Events.Commands;

/// <summary>Validates <see cref="CreateEventCommand"/>.</summary>
public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EventType).NotEmpty().Must(t => ValidEventTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("EventType must be one of: Wedding, Reception, Birthday, Corporate, Outdoor, Other");
        RuleFor(x => x.EventDate).NotEmpty();
        RuleFor(x => x.WatchFolder).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.VenueName).MaximumLength(200).When(x => x.VenueName is not null);
        RuleFor(x => x.ClientName).MaximumLength(200).When(x => x.ClientName is not null);
        RuleFor(x => x.CreatedBy).NotEmpty();
    }

    private static readonly string[] ValidEventTypes = ["Wedding", "Reception", "Birthday", "Corporate", "Outdoor", "Other"];
}
