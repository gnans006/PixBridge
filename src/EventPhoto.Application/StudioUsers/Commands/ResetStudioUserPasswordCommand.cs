using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace EventPhoto.Application.StudioUsers.Commands;

/// <summary>Resets a studio user's password (owner-only action).</summary>
public sealed record ResetStudioUserPasswordCommand(Guid UserId, string NewPassword)
    : IRequest<Result>;

public sealed class ResetStudioUserPasswordCommandValidator : AbstractValidator<ResetStudioUserPasswordCommand>
{
    public ResetStudioUserPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public sealed class ResetStudioUserPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ResetStudioUserPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetStudioUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
