using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Users;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.StudioUsers.Commands;

/// <summary>Updates an existing studio user's profile and role.</summary>
public sealed record UpdateStudioUserCommand(
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    string Role) : IRequest<Result<StudioUserResponse>>;

public sealed class UpdateStudioUserCommandValidator : AbstractValidator<UpdateStudioUserCommand>
{
    public UpdateStudioUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
        RuleFor(x => x.Role).NotEmpty()
            .Must(r => Enum.TryParse<UserRole>(r, true, out _))
            .WithMessage("Role must be one of: StudioOwner, StudioManager, Operator.");
    }
}

public sealed class UpdateStudioUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateStudioUserCommand, Result<StudioUserResponse>>
{
    public async Task<Result<StudioUserResponse>> Handle(
        UpdateStudioUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<StudioUserResponse>("User not found.");

        if (await userRepository.ExistsByEmailAsync(request.Email.ToLowerInvariant(), request.UserId, cancellationToken))
            return Result.Failure<StudioUserResponse>($"Email '{request.Email}' is already registered to another account.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            return Result.Failure<StudioUserResponse>("Invalid role.");

        // Prevent demoting the last owner
        if (user.Role.IsOwner() && !role.IsOwner())
        {
            var ownerCount = await userRepository.CountOwnerAccountsAsync(cancellationToken);
            if (ownerCount <= 1)
                return Result.Failure<StudioUserResponse>("Cannot change role: at least one StudioOwner must remain.");
        }

        user.Update(request.FullName, request.Email, request.Phone);
        user.UpdateRole(role);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new StudioUserResponse(
            user.Id, user.DisplayName, user.Username, user.Email, user.Phone,
            user.Role.ToClaimValue(), user.IsActive, user.LastLoginAt, user.CreatedAt, user.UpdatedAt));
    }
}
