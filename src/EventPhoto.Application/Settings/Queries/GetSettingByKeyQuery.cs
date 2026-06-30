using AutoMapper;
using EventPhoto.Contracts.Responses.Settings;
using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Settings.Queries;

/// <summary>
/// Query that retrieves a single system setting by its key.
/// </summary>
/// <param name="Key">The setting key.</param>
public sealed record GetSettingByKeyQuery(string Key) : IRequest<Result<SystemSettingResponse?>>;

/// <summary>
/// Handles the <see cref="GetSettingByKeyQuery"/>.
/// </summary>
public sealed class GetSettingByKeyQueryHandler(
    ISystemSettingRepository settingRepository,
    IMapper mapper)
    : IRequestHandler<GetSettingByKeyQuery, Result<SystemSettingResponse?>>
{
    /// <inheritdoc />
    public async Task<Result<SystemSettingResponse?>> Handle(
        GetSettingByKeyQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByKeyAsync(request.Key, cancellationToken);
        if (setting is null)
        {
            return Result.Success<SystemSettingResponse?>(null);
        }

        return Result.Success<SystemSettingResponse?>(mapper.Map<SystemSettingResponse>(setting));
    }
}
