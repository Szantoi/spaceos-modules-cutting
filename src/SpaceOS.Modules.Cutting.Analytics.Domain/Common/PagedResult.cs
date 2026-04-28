namespace SpaceOS.Modules.Cutting.Analytics.Domain.Common;

/// <summary>Cursor-free paginated result envelope.</summary>
/// <typeparam name="T">Type of each item in the page.</typeparam>
public sealed class AnalyticsPagedResult<T>
{
    /// <summary>Items in the current page.</summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>Total matching records across all pages (count query result).</summary>
    public int TotalCount { get; }

    /// <summary>Number of items skipped before this page.</summary>
    public int Skip { get; }

    /// <summary>Maximum items requested for this page.</summary>
    public int Take { get; }

    /// <summary>
    /// <see langword="true"/> when more items exist after this page.
    /// </summary>
    public bool HasNextPage => Skip + Items.Count < TotalCount;


    /// <summary>Creates a new <see cref="AnalyticsPagedResult{T}"/>.</summary>
    public AnalyticsPagedResult(IReadOnlyList<T> items, int totalCount, int skip, int take)
    {
        Items = items;
        TotalCount = totalCount;
        Skip = skip;
        Take = take;
    }
}
