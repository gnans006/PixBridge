using FluentValidation;

namespace EventPhoto.Application.Settings.Commands;

/// <summary>Validates <see cref="UpdateSettingCommand"/>.</summary>
public sealed class UpdateSettingCommandValidator : AbstractValidator<UpdateSettingCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public UpdateSettingCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Setting key is required.")
            .MaximumLength(200).WithMessage("Setting key must not exceed 200 characters.")
            .Matches(@"^[a-zA-Z0-9._\-]+$").WithMessage("Setting key may only contain letters, digits, dots, underscores, and hyphens.");

        RuleFor(x => x.Value)
            .NotNull().WithMessage("Setting value is required.")
            .MaximumLength(2000).WithMessage("Setting value must not exceed 2000 characters.");
    }
}
