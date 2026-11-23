using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P.Pager
{
    /// <summary>
    /// Paging (and optional sorting) values from the request, for example <c>?page=2&amp;pageSize=50&amp;sort=-created</c>.
    /// Binds automatically as an MVC action parameter, with <c>[FromQuery]</c>, or with <c>[AsParameters]</c> in Minimal APIs.
    /// Values are untrusted: use <see cref="GetPageIndex"/> and <see cref="GetPageSize"/> (or the extension methods that take a
    /// <see cref="PageRequest"/>), which apply safe defaults and a maximum page size.
    /// </summary>
    /// <example>
    /// <code>
    /// app.MapGet("/members", ([AsParameters] PageRequest request, AppDb db) =&gt;
    ///     db.Members.ApplySort(request.Sort, MemberSorts).ToPagerList(request).ToPagedResult());
    /// </code>
    /// </example>
    public class PageRequest
    {
        /// <summary>The default page size when none (or an invalid one) is requested: 10.</summary>
        public const int DefaultPageSize = 10;

        /// <summary>The default maximum page size: 100.</summary>
        public const int DefaultMaxPageSize = 100;

        /// <summary>Requested page (1-based). Query string name: page.</summary>
        public int? Page { get; set; }

        /// <summary>Requested page size. Query string name: pageSize.</summary>
        public int? PageSize { get; set; }

        /// <summary>Requested sort, for example <c>name,-created</c>. Apply it with <see cref="SortExtension.ApplySort"/>. Query string name: sort.</summary>
        public string? Sort { get; set; }

        /// <summary>The requested page, or 1 when missing or below 1.</summary>
        /// <returns>A page index of at least 1.</returns>
        public int GetPageIndex() => Page.HasValue && Page.Value >= 1 ? Page.Value : 1;

        /// <summary>The requested page size, defaulted and capped. See <see cref="PagerExtension.ClampPageSize"/>.</summary>
        /// <param name="defaultPageSize">Used when no valid size is requested.</param>
        /// <param name="maxPageSize">The largest size allowed.</param>
        /// <returns>A page size between 1 and <paramref name="maxPageSize"/>.</returns>
        public int GetPageSize(int defaultPageSize = DefaultPageSize, int maxPageSize = DefaultMaxPageSize) =>
            PagerExtension.ClampPageSize(PageSize, defaultPageSize, maxPageSize);
    }

    /// <summary>
    /// Paging overloads that take a <see cref="PageRequest"/>.
    /// </summary>
    public static class PageRequestExtension
    {
        /// <summary>Pages the query using the request's page and a safe page size.</summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source query. Order it first (for example with <see cref="SortExtension.ApplySort"/>).</param>
        /// <param name="request">The paging request.</param>
        /// <param name="defaultPageSize">Used when no valid size is requested. By default 10.</param>
        /// <param name="maxPageSize">The largest size allowed. By default 100.</param>
        /// <returns>The page.</returns>
        public static IPager<T> ToPagerList<T>(this IQueryable<T> source, PageRequest request,
            int defaultPageSize = PageRequest.DefaultPageSize, int maxPageSize = PageRequest.DefaultMaxPageSize)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return source.ToPagerList(request.GetPageIndex(), request.GetPageSize(defaultPageSize, maxPageSize));
        }

        /// <summary>Pages an in-memory sequence using the request's page and a safe page size.</summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="request">The paging request.</param>
        /// <param name="defaultPageSize">Used when no valid size is requested. By default 10.</param>
        /// <param name="maxPageSize">The largest size allowed. By default 100.</param>
        /// <returns>The page.</returns>
        public static IPager<T> ToPagerList<T>(this IEnumerable<T> source, PageRequest request,
            int defaultPageSize = PageRequest.DefaultPageSize, int maxPageSize = PageRequest.DefaultMaxPageSize)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return source.ToPagerList(request.GetPageIndex(), request.GetPageSize(defaultPageSize, maxPageSize));
        }

        /// <summary>Asynchronously pages the query using the request's page and a safe page size, with the provider's async operators.</summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source query. Order it first.</param>
        /// <param name="request">The paging request.</param>
        /// <param name="countAsync">Asynchronously counts the items, for example <c>(q, ct) =&gt; q.CountAsync(ct)</c>.</param>
        /// <param name="toListAsync">Asynchronously materializes the query, for example <c>(q, ct) =&gt; q.ToListAsync(ct)</c>.</param>
        /// <param name="defaultPageSize">Used when no valid size is requested. By default 10.</param>
        /// <param name="maxPageSize">The largest size allowed. By default 100.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>The page.</returns>
        public static Task<IPager<T>> ToPagerListAsync<T>(this IQueryable<T> source, PageRequest request,
            Func<IQueryable<T>, CancellationToken, Task<int>> countAsync,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync,
            int defaultPageSize = PageRequest.DefaultPageSize, int maxPageSize = PageRequest.DefaultMaxPageSize,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return source.ToPagerListAsync(request.GetPageIndex(), request.GetPageSize(defaultPageSize, maxPageSize), countAsync, toListAsync, cancellationToken);
        }

        /// <summary>Returns the requested page without counting the total. See <see cref="SliceExtension.ToSlice{T}(IQueryable{T}, int, int)"/>.</summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="source">The source query. Order it first.</param>
        /// <param name="request">The paging request.</param>
        /// <param name="defaultPageSize">Used when no valid size is requested. By default 10.</param>
        /// <param name="maxPageSize">The largest size allowed. By default 100.</param>
        /// <returns>The page.</returns>
        public static ISlice<T> ToSlice<T>(this IQueryable<T> source, PageRequest request,
            int defaultPageSize = PageRequest.DefaultPageSize, int maxPageSize = PageRequest.DefaultMaxPageSize)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return source.ToSlice(request.GetPageIndex(), request.GetPageSize(defaultPageSize, maxPageSize));
        }
    }
}
