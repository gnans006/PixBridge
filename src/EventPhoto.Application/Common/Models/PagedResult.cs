namespace EventPhoto.Application.Common.Models;

/// <summary>Wraps a paginated list of items with total count metadata.</summary>
/// <typeparam name="T">Item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total number of items across all pages.</summary>
    public int TotalCount { get; }

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>Whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Initializes a new instance of <see cref="PagedResult{T}"/>.</summary>
    public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
