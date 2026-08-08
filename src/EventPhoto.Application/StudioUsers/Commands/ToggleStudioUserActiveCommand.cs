using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.StudioUsers.Commands;

/// <summary>Deactivates a studio user account.</summary>
public sealed record DeactivateStudioUserCommand(Guid UserId) : IRequest<Result>;

public sealed class DeactivateStudioUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateStudioUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateStudioUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure("User not found.");

        if (user.Role.IsOwner())
        {
            var ownerCount = await userRepository.CountOwnerAccountsAsync(cancellationToken);
            if (ownerCount <= 1)
                return Result.Failure("Cannot deactivate: at least one active StudioOwner must remain.");
        }

        user.Deactivate();
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>Activates a previously deactivated studio user account.</summary>
public sealed record ActivateStudioUserCommand(Guid UserId) : IRequest<Result>;

public sealed class ActivateStudioUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ActivateStudioUserCommand, Result>
{
    public async Task<Result> Handle(ActivateStudioUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure("User not found.");

        user.Activate();
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
