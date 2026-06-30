using EventPhoto.Domain.Common;
using EventPhoto.Domain.Entities;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Settings.Commands;

/// <summary>
/// Command that creates a new system setting or updates the value of an existing one.
/// </summary>
/// <param name="Key">The unique setting key.</param>
/// <param name="Value">The new setting value.</param>
/// <param name="Description">Optional human-readable description.</param>
public sealed record UpsertSettingCommand(
    string Key,
    string Value,
    string? Description = null)
    : IRequest<Result>;

/// <summary>
/// Handles the <see cref="UpsertSettingCommand"/>.
/// </summary>
public sealed class UpsertSettingCommandHandler(
    ISystemSettingRepository settingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpsertSettingCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(
        UpsertSettingCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await settingRepository.GetByKeyAsync(request.Key, cancellationToken);

        if (existing is null)
        {
            var setting = SystemSetting.Create(request.Key, request.Value, request.Description);
            await settingRepository.AddAsync(setting, cancellationToken);
        }
        else
        {
            existing.UpdateValue(request.Value);
            await settingRepository.UpdateAsync(existing, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
