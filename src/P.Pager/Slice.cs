using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P.Pager
{
    /// <summary>
    /// A page fetched without counting the total. It knows whether there is a next page, but not how many pages there are.
    /// Use it when COUNT(*) is too slow (large tables) and a "Previous / Next" pager is enough.
    /// </summary>
    public interface ISlice
    {
        /// <summary>Index (1-based) of the current page.</summary>
        int CurrentPageIndex { get; }

        /// <summary>Maximum number of items per page.</summary>
        int PageSize { get; }

        /// <summary>True if this is not the first page.</summary>
        bool HasPreviousPage { get; }

        /// <summary>True if at least one item exists after this page.</summary>
        bool HasNextPage { get; }

        /// <summary>1-based index of the first item on this page (0 when the page is empty).</summary>
        int StartItemIndex { get; }

        /// <summary>1-based index of the last item on this page (0 when the page is empty).</summary>
        int EndItemIndex { get; }
    }

    /// <summary>
    /// A page of items fetched without counting the total. See <see cref="ISlice"/>.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    public interface ISlice<out T> : IReadOnlyList<T>, ISlice
    {
    }

    internal sealed class Slice<T> : ISlice<T>
    {
        private readonly List<T> _items;

        public Slice(List<T> items, int currentPageIndex, int pageSize, bool hasNextPage)
        {
            _items = items;
            CurrentPageIndex = currentPageIndex;
            PageSize = pageSize;
            HasNextPage = hasNextPage;
        }

        public int CurrentPageIndex { get; }
        public int PageSize { get; }
        public bool HasPreviousPage => CurrentPageIndex > 1;
        public bool HasNextPage { get; }
        public int StartItemIndex => _items.Count == 0 ? 0 : (int)Math.Min(int.MaxValue, (CurrentPageIndex - 1L) * PageSize + 1);
        public int EndItemIndex => _items.Count == 0 ? 0 : (int)Math.Min(int.MaxValue, (CurrentPageIndex - 1L) * PageSize + _items.Count);
        public int Count => _items.Count;
        public T this[int index] => _items[index];
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Count-free paging: fetches one extra row to find out whether there is a next page, and never runs COUNT(*).
    /// </summary>
    public static class SliceExtension
    {
        /// <summary>
        /// Returns the requested page without counting the total. A page past the end is returned empty (with <see cref="ISlice.HasPreviousPage"/> true).
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source. Order it first when it comes from a database.</param>
        /// <param name="pageIndex">Index (1-based) of the page. Values below 1 return the first page.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <returns>The page.</returns>
        public static ISlice<T> ToSlice<T>(this IQueryable<T> source, int pageIndex = 1, int pageSize = 10)
        {
            IQueryable<T> query = BuildQuery(source, ref pageIndex, pageSize);
            return CreateSlice(query.ToList(), pageIndex, pageSize);
        }

        /// <summary>
        /// Returns the requested page of an in-memory sequence without counting it.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="pageIndex">Index (1-based) of the page. Values below 1 return the first page.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <returns>The page.</returns>
        public static ISlice<T> ToSlice<T>(this IEnumerable<T> source, int pageIndex = 1, int pageSize = 10)
        {
            if (source is IQueryable<T> query)
                return query.ToSlice(pageIndex, pageSize);
            if (source == null) throw new ArgumentNullException(nameof(source));
            PagerExtension.ValidatePageSize(pageSize);
            pageIndex = ClampToOffsetRange(pageIndex, pageSize);
            return CreateSlice(source.Skip((pageIndex - 1) * pageSize).Take(pageSize + 1).ToList(), pageIndex, pageSize);
        }

        /// <summary>
        /// Asynchronously returns the requested page without counting the total, using the provider's own async operator
        /// (for example <c>(q, ct) =&gt; q.ToListAsync(ct)</c> with Entity Framework Core).
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source query. Order it first.</param>
        /// <param name="pageIndex">Index (1-based) of the page. Values below 1 return the first page.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <param name="toListAsync">Asynchronously materializes the query.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>The page.</returns>
        public static async Task<ISlice<T>> ToSliceAsync<T>(this IQueryable<T> source, int pageIndex, int pageSize,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync, CancellationToken cancellationToken = default)
        {
            if (toListAsync == null) throw new ArgumentNullException(nameof(toListAsync));
            IQueryable<T> query = BuildQuery(source, ref pageIndex, pageSize);
            List<T> items = await toListAsync(query, cancellationToken).ConfigureAwait(false);
            return CreateSlice(items, pageIndex, pageSize);
        }

        private static IQueryable<T> BuildQuery<T>(IQueryable<T> source, ref int pageIndex, int pageSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            PagerExtension.ValidatePageSize(pageSize);
            PagerExtension.CheckOrdering(source);
            pageIndex = ClampToOffsetRange(pageIndex, pageSize);
            return source.Skip((pageIndex - 1) * pageSize).Take(pageSize + 1); // one extra row tells us whether there is a next page
        }

        /// <summary>Keeps the page at least 1 and its offset within int range (no count, so no "last page" to clamp to).</summary>
        private static int ClampToOffsetRange(int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
                return 1;
            int maxPage = (int)Math.Min(int.MaxValue, (long)int.MaxValue / pageSize + 1);
            return Math.Min(pageIndex, maxPage);
        }

        private static Slice<T> CreateSlice<T>(List<T> items, int pageIndex, int pageSize)
        {
            bool hasNext = items.Count > pageSize;
            if (hasNext)
                items.RemoveAt(items.Count - 1);
            return new Slice<T>(items, pageIndex, pageSize, hasNext);
        }
    }
}
