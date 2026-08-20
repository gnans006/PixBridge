using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Application.Common.Models;
using EventPhoto.Contracts.Responses.Users;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Exceptions;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.StudioUsers.Commands;

/// <summary>Creates a new studio user account.</summary>
public sealed record CreateStudioUserCommand(
    string FullName,
    string Username,
    string Email,
    string? Phone,
    string Role,
    string Password) : IRequest<Result<StudioUserResponse>>, IRequiresFeature
{
    /// <inheritdoc />
    public string FeatureKey => Common.Models.FeatureKey.Users;
}

public sealed class CreateStudioUserCommandValidator : AbstractValidator<CreateStudioUserCommand>
{
    public CreateStudioUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Username may only contain letters, digits, dots, underscores and hyphens.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => Enum.TryParse<UserRole>(r, true, out _))
            .WithMessage("Role must be one of: StudioOwner, StudioManager, Operator.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class CreateStudioUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateStudioUserCommand, Result<StudioUserResponse>>
{
    public async Task<Result<StudioUserResponse>> Handle(
        CreateStudioUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByUsernameAsync(request.Username.ToLowerInvariant(), null, cancellationToken))
            return Result.Failure<StudioUserResponse>($"Username '{request.Username}' is already taken.");

        if (await userRepository.ExistsByEmailAsync(request.Email.ToLowerInvariant(), null, cancellationToken))
            return Result.Failure<StudioUserResponse>($"Email '{request.Email}' is already registered.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return Result.Failure<StudioUserResponse>("Invalid role.");

        var hash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Username, request.Email, hash, role, request.FullName, request.Phone);

        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new StudioUserResponse(
            user.Id, user.DisplayName, user.Username, user.Email, user.Phone,
            user.Role.ToClaimValue(), user.IsActive, user.LastLoginAt, user.CreatedAt, user.UpdatedAt));
    }
}
