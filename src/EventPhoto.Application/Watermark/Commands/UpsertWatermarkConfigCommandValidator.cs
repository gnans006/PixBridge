using EventPhoto.Domain.Enums;
using FluentValidation;

namespace EventPhoto.Application.Watermark.Commands;

/// <summary>Validates <see cref="UpsertWatermarkConfigCommand"/>.</summary>
public sealed class UpsertWatermarkConfigCommandValidator : AbstractValidator<UpsertWatermarkConfigCommand>
{
    private static readonly string[] ValidModes =
        Enum.GetNames<WatermarkMode>();

    private static readonly string[] ValidStyles =
        Enum.GetNames<WatermarkStyle>();

    private static readonly string[] ValidScales =
        Enum.GetNames<WatermarkScale>();

    /// <summary>Initialises validation rules.</summary>
    public UpsertWatermarkConfigCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("EventId is required.");

        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("Mode is required.")
            .Must(m => ValidModes.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Mode must be one of: {string.Join(", ", ValidModes)}.");

        RuleFor(x => x.Style)
            .NotEmpty().WithMessage("Style is required.")
            .Must(s => ValidStyles.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Style must be one of: {string.Join(", ", ValidStyles)}.");

        RuleFor(x => x.Scale)
            .NotEmpty().WithMessage("Scale is required.")
            .Must(s => ValidScales.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Scale must be one of: {string.Join(", ", ValidScales)}.");

        RuleFor(x => x.Opacity)
            .InclusiveBetween(0f, 1f).WithMessage("Opacity must be between 0.0 and 1.0.");

        RuleFor(x => x.CustomText)
            .NotEmpty().WithMessage("CustomText is required when Mode is CustomText.")
            .MaximumLength(500).WithMessage("CustomText must not exceed 500 characters.")
            .When(x => string.Equals(x.Mode, nameof(WatermarkMode.CustomText), StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Template)
            .NotEmpty().WithMessage("Template is required when Mode is DynamicTemplate.")
            .MaximumLength(1000).WithMessage("Template must not exceed 1000 characters.")
            .When(x => string.Equals(x.Mode, nameof(WatermarkMode.DynamicTemplate), StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.LogoPath)
            .MaximumLength(1024).WithMessage("LogoPath must not exceed 1024 characters.")
            .Must(p => p is null || !p.Contains("..")).WithMessage("LogoPath must not contain path traversal sequences.")
            .When(x => x.LogoPath is not null);

        RuleFor(x => x.TextColor)
            .NotEmpty().WithMessage("TextColor is required.")
            .Matches(@"^#[0-9A-Fa-f]{3}([0-9A-Fa-f]{3})?$")
            .WithMessage("TextColor must be a valid hex colour (e.g. #FFF or #FFFFFF).");

        RuleFor(x => x.FontName)
            .MaximumLength(100).WithMessage("FontName must not exceed 100 characters.")
            .When(x => x.FontName is not null);

        RuleFor(x => x.BackgroundOpacity)
            .InclusiveBetween(0f, 1f).WithMessage("BackgroundOpacity must be between 0.0 and 1.0.");
    }
}
