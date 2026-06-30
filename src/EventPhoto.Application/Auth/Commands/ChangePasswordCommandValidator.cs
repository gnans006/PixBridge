using FluentValidation;

namespace EventPhoto.Application.Auth.Commands;

/// <summary>Validates <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmNewPassword).NotEmpty().Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
