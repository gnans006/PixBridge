using EventPhoto.Application.Common.Interfaces;
using EventPhoto.Domain.Common;
using MediatR;

namespace EventPhoto.Application.ApplicationSettings.Queries;

/// <summary>Returns a snapshot of the current machine's LAN network information.</summary>
public sealed record GetCurrentNetworkInformationQuery(int Port = 5000)
    : IRequest<Result<NetworkInformationDto>>;

/// <summary>Projection of the current network state.</summary>
public sealed record NetworkInformationDto(
    string HostName,
    string MachineName,
    string PrimaryIpAddress,
    int Port,
    IReadOnlyList<string> AllIpAddresses,
    string AccessibleLanUrl,
    bool IsLanReachable);

/// <summary>Handles <see cref="GetCurrentNetworkInformationQuery"/>.</summary>
public sealed class GetCurrentNetworkInformationQueryHandler(
    INetworkInformationService networkService)
    : IRequestHandler<GetCurrentNetworkInformationQuery, Result<NetworkInformationDto>>
{
    /// <inheritdoc />
    public Task<Result<NetworkInformationDto>> Handle(
        GetCurrentNetworkInformationQuery request,
        CancellationToken cancellationToken)
    {
        var info = networkService.GetCurrentNetworkInformation(request.Port);

        var dto = new NetworkInformationDto(
            info.HostName,
            info.MachineName,
            info.PrimaryIpAddress,
            info.Port,
            info.AllIpAddresses,
            info.AccessibleLanUrl,
            info.IsLanReachable);

        return Task.FromResult(Result.Success(dto));
    }
}
