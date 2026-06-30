using EventPhoto.Domain.Common;
using EventPhoto.Domain.Interfaces;
using MediatR;

namespace EventPhoto.Application.Settings.Commands;

/// <summary>Command to update a system setting value by key.</summary>
/// <param name="Key">The setting key.</param>
/// <param name="Value">The new value.</param>
public sealed record UpdateSettingCommand(string Key, string Value) : IRequest<Result>;

/// <summary>Handles the <see cref="UpdateSettingCommand"/>.</summary>
public sealed class UpdateSettingCommandHandler(
    ISystemSettingRepository settingRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSettingCommand, Result>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await settingRepository.GetByKeyAsync(request.Key, cancellationToken);
        if (setting is null)
        {
            return Result.Failure($"Setting '{request.Key}' not found.");
        }

        setting.UpdateValue(request.Value);
        await settingRepository.UpdateAsync(setting, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
