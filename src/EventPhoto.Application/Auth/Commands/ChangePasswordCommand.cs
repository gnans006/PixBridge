using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Auth.Commands;

/// <summary>Command to change an admin user's password.</summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="CurrentPassword">The current plain-text password for verification.</param>
/// <param name="NewPassword">The new plain-text password.</param>
/// <param name="ConfirmNewPassword">Confirmation for the new plain-text password.</param>
public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmNewPassword) : IRequest<Result>;

/// <summary>Handles the <see cref="ChangePasswordCommand"/>.</summary>
public sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return Result.Failure("New password and confirmation do not match.");
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("Current password is incorrect.");
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
