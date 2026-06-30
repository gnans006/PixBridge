using FluentValidation;

namespace EventPhoto.Application.Auth.Commands;

/// <summary>Validates <see cref="LoginCommand"/>.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
