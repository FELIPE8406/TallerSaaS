using Microsoft.EntityFrameworkCore;
using TallerSaaS.Application.DTOs;

namespace TallerSaaS.Application.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Extension method to automatically apply OFFSET and FETCH NEXT to an IQueryable,
    /// returning a PagedResult containing the items and metadata.
    /// Ensure the target IQueryable is ordered before calling this method to guarantee consistent OFFSETs.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The unordered or ordered query.</param>
    /// <param name="pageNumber">The 1-based page index.</param>
    /// <param name="pageSize">The maximum number of items per page.</param>
    /// <returns>A PagedResult object.</returns>
    public static async Task<PagedResult<T>> ToPagedListAsync<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;

        var count = await query.CountAsync();
        
        // EF Core will translate this exactly to: OFFSET X ROWS FETCH NEXT Y ROWS ONLY
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Data = items,
            TotalCount = count,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
