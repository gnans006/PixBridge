namespace EventPhoto.Contracts.Common;

/// <summary>
/// Wraps a paginated set of items returned from a list query.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <param name="Items">The items on the current page.</param>
/// <param name="TotalCount">Total number of matching records across all pages.</param>
/// <param name="Page">The 1-based current page number.</param>
/// <param name="PageSize">The requested page size.</param>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>
    /// Gets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNextPage => Page * PageSize < TotalCount;

    /// <summary>
    /// Gets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
