using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Settings.Queries;

/// <summary>Query to retrieve all system settings.</summary>
public sealed record GetAllSettingsQuery : IRequest<Result<List<SystemSetting>>>;

/// <summary>Handles fetching all system settings.</summary>
public sealed class GetAllSettingsQueryHandler(ISystemSettingRepository settingRepository)
    : IRequestHandler<GetAllSettingsQuery, Result<List<SystemSetting>>>
{
    /// <inheritdoc />
    public async Task<Result<List<SystemSetting>>> Handle(
        GetAllSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await settingRepository.GetAllAsync(cancellationToken);
        return Result.Success(settings);
    }
}
