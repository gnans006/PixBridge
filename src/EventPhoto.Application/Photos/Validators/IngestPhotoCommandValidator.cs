using EventPhoto.Application.Photos.Commands;
using FluentValidation;

namespace EventPhoto.Application.Photos.Validators;

/// <summary>
/// Validates an <see cref="IngestPhotoCommand"/> before the handler runs.
/// </summary>
public sealed class IngestPhotoCommandValidator : AbstractValidator<IngestPhotoCommand>
{
    /// <summary>
    /// Initializes validation rules for <see cref="IngestPhotoCommand"/>.
    /// </summary>
    public IngestPhotoCommandValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("EventId is required.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("FileName is required.")
            .MaximumLength(512)
            .WithMessage("FileName must not exceed 512 characters.");

        RuleFor(x => x.OriginalPath)
            .NotEmpty()
            .WithMessage("OriginalPath is required.");

        RuleFor(x => x.ThumbnailPath)
            .NotEmpty()
            .WithMessage("ThumbnailPath is required.");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThanOrEqualTo(0)
            .WithMessage("FileSizeBytes must be zero or greater.");

        RuleFor(x => x.MimeType)
            .NotEmpty()
            .WithMessage("MimeType is required.")
            .Must(m => m.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("MimeType must be an image MIME type.");
    }
}
