namespace EventPhoto.Contracts.Requests.Photos;

/// <summary>Request model for querying photos for a gallery page.</summary>
public sealed record GetPhotosRequest(Guid EventId, int Page = 1, int PageSize = 50);
