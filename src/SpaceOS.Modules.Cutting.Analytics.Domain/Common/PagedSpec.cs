using Ardalis.Specification;

namespace SpaceOS.Modules.Cutting.Analytics.Domain.Common;

/// <summary>
/// Base specification that wires skip/take pagination onto the query.
/// Derived specs add WHERE and ORDER BY clauses.
/// </summary>
/// <typeparam name="T">Entity type being queried.</typeparam>
public abstract class PagedSpec<T> : Specification<T> where T : class
{
    /// <summary>Applies <paramref name="skip"/> and <paramref name="take"/> to the query.</summary>
    protected PagedSpec(int skip, int take)
    {
        Query.Skip(skip).Take(take);
    }
}
