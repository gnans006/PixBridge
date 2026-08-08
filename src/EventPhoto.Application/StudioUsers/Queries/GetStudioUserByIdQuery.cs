using EventPhoto.Contracts.Responses.Users;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.StudioUsers.Queries;

/// <summary>Returns a single studio user by identifier.</summary>
public sealed record GetStudioUserByIdQuery(Guid UserId) : IRequest<Result<StudioUserResponse>>;

public sealed class GetStudioUserByIdQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetStudioUserByIdQuery, Result<StudioUserResponse>>
{
    public async Task<Result<StudioUserResponse>> Handle(
        GetStudioUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<StudioUserResponse>("User not found.");

        return Result.Success(new StudioUserResponse(
            user.Id, user.DisplayName, user.Username, user.Email, user.Phone,
            user.Role.ToClaimValue(), user.IsActive, user.LastLoginAt, user.CreatedAt, user.UpdatedAt));
    }
}
