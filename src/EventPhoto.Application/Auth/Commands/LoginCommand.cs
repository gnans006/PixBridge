using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Contracts.Responses.Auth;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Auth.Commands;

/// <summary>
/// Command that authenticates a user by username and password and returns a JWT bearer token.
/// </summary>
/// <param name="Username">The account username.</param>
/// <param name="Password">The plain-text password to verify.</param>
public sealed record LoginCommand(string Username, string Password)
    : IRequest<Result<LoginResponse>>;

/// <summary>
/// Handles the <see cref="LoginCommand"/> by validating credentials and issuing a JWT token.
/// </summary>
public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    /// <inheritdoc />
    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(
            request.Username.ToLowerInvariant(),
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<LoginResponse>("Invalid username or password.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>("Invalid username or password.");
        }

        user.RecordLogin();
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = tokenService.GenerateToken(user);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);

        return Result.Success(new LoginResponse(
            token,
            user.Username,
            user.Role.ToString(),
            expiresAt));
    }
}
