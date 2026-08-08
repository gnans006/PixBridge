using EventPhoto.Contracts.Responses.Users;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Enums;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.StudioUsers.Queries;

/// <summary>Returns all studio user accounts.</summary>
public sealed record GetAllStudioUsersQuery : IRequest<Result<List<StudioUserResponse>>>;

public sealed class GetAllStudioUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetAllStudioUsersQuery, Result<List<StudioUserResponse>>>
{
    public async Task<Result<List<StudioUserResponse>>> Handle(
        GetAllStudioUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var response = users.Select(u => new StudioUserResponse(
            u.Id, u.DisplayName, u.Username, u.Email, u.Phone,
            u.Role.ToClaimValue(), u.IsActive, u.LastLoginAt, u.CreatedAt, u.UpdatedAt))
            .ToList();
        return Result.Success(response);
    }
}
