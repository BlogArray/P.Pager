using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace P.Pager
{
    /// <summary>
    /// Extension methods that split an <see cref="IEnumerable{T}"/> or <see cref="IQueryable{T}"/> into pages.
    /// </summary>
    public static class PagerExtension
    {
        /// <summary>
        /// Page-size check for the methods that existed in 9.0: negative sizes throw (as in 9.0), and 0 returns an empty page
        /// instead of throwing, so apps that pass 0 keep working. New methods use <see cref="ValidatePageSize"/> (must be at least 1).
        /// </summary>
        private static void ValidateLegacyPageSize(int pageSize)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "pageSize cannot be negative.");
        }

        internal static void ValidatePageSize(int pageSize)
        {
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "pageSize cannot be less than 1.");
        }

        /// <summary>
        /// Clamps the requested page into [1, last page]. An empty source has a single (empty) page 1.
        /// Computed directly (no loop) and in 64-bit math so huge page numbers can't overflow or spin.
        /// </summary>
        internal static int ClampPageIndex(int pageIndex, int pageSize, int totalItemCount)
        {
            if (pageIndex < 1)
                return 1;

            int lastPage = (int)Math.Max(1, (totalItemCount + (long)pageSize - 1) / pageSize);
            return Math.Min(pageIndex, lastPage);
        }

        /// <summary>
        /// Turns an untrusted page size (for example from the query string) into a safe one:
        /// null or values below 1 become <paramref name="defaultPageSize"/>, values above <paramref name="maxPageSize"/> are capped.
        /// </summary>
        /// <example>
        /// <code>
        /// var pager = query.ToPagerList(page, PagerExtension.ClampPageSize(pageSize, defaultPageSize: 20, maxPageSize: 100));
        /// </code>
        /// </example>
        /// <param name="pageSize">The requested page size.</param>
        /// <param name="defaultPageSize">Used when <paramref name="pageSize"/> is null or below 1. By default 10.</param>
        /// <param name="maxPageSize">The largest page size allowed. By default 100.</param>
        /// <returns>A page size between 1 and <paramref name="maxPageSize"/>.</returns>
        public static int ClampPageSize(int? pageSize, int defaultPageSize = 10, int maxPageSize = 100)
        {
            if (maxPageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(maxPageSize), maxPageSize, "maxPageSize cannot be less than 1.");
            if (defaultPageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(defaultPageSize), defaultPageSize, "defaultPageSize cannot be less than 1.");

            int size = pageSize.HasValue && pageSize.Value >= 1 ? pageSize.Value : defaultPageSize;
            return Math.Min(size, maxPageSize);
        }

        /// <summary>
        /// Pages an in-memory sequence with LINQ to Objects. Avoids AsQueryable() (not trimming/AOT safe, and slower),
        /// and enumerates lazy sequences only once.
        /// </summary>
        private static IPager<T> PageInMemory<T>(IEnumerable<T> allItems, int pageIndex, int pageSize)
        {
            if (allItems == null) throw new ArgumentNullException(nameof(allItems));
            ValidateLegacyPageSize(pageSize);

            IReadOnlyCollection<T> items = allItems as IReadOnlyCollection<T> ?? allItems.ToList();
            if (pageSize == 0)
                return new PagerAsync<T>([], Math.Max(1, pageIndex), 0, items.Count);
            pageIndex = ClampPageIndex(pageIndex, pageSize, items.Count);
            int itemIndex = (pageIndex - 1) * pageSize;
            return new PagerAsync<T>(items.Skip(itemIndex).Take(pageSize), pageIndex, pageSize, items.Count);
        }

        internal static void CheckOrdering<T>(IQueryable<T> query)
        {
            if (PagerSettings.UnorderedQueryBehavior == UnorderedQueryBehavior.Ignore || query is EnumerableQuery)
                return;

            OrderByFinder finder = new();
            finder.Visit(query.Expression);
            if (finder.Found)
                return;

            string message = $"P.Pager: paging an unordered query of {typeof(T).Name} from {query.Provider.GetType().Name}. " +
                             "Add OrderBy(...) before paging, otherwise pages may repeat or skip items.";
            if (PagerSettings.UnorderedQueryBehavior == UnorderedQueryBehavior.Throw)
                throw new InvalidOperationException(message);
            Trace.TraceWarning(message);
        }

        private sealed class OrderByFinder : ExpressionVisitor
        {
            public bool Found { get; private set; }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.DeclaringType == typeof(Queryable) &&
                    (node.Method.Name.StartsWith("OrderBy", StringComparison.Ordinal) || node.Method.Name.StartsWith("ThenBy", StringComparison.Ordinal) || node.Method.Name == "Order" || node.Method.Name == "OrderDescending"))
                {
                    Found = true;
                    return node;
                }
                return base.VisitMethodCall(node);
            }
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list.</param>
        /// <param name="pageSize">Size of individual child.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static IPager<T> AsPagerList<T>(this IQueryable<T> allItems, int pageIndex, int pageSize)
        {
            ValidateLegacyPageSize(pageSize);
            CheckOrdering(allItems);
            int totalItemCount = allItems.Count();
            if (pageSize == 0)
                return new PagerAsync<T>([], Math.Max(1, pageIndex), 0, totalItemCount);
            pageIndex = ClampPageIndex(pageIndex, pageSize, totalItemCount);
            int itemIndex = (pageIndex - 1) * pageSize;
            IQueryable<T> pageOfItems = allItems.Skip(itemIndex).Take(pageSize);
            return new Pager<T>(pageOfItems, pageIndex, pageSize, totalItemCount);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list.</param>
        /// <param name="pageSize">Size of individual child.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        private static async Task<IPager<T>> AsPagerListAsync<T>(IQueryable<T> allItems, int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            ValidateLegacyPageSize(pageSize);
            CheckOrdering(allItems);
            List<T> childSet = [];
            int totalItemCount = allItems.Count();
            if (pageSize == 0)
                return new PagerAsync<T>(childSet, Math.Max(1, pageIndex), 0, totalItemCount);
            pageIndex = ClampPageIndex(pageIndex, pageSize, totalItemCount);
            int itemIndex = (pageIndex - 1) * pageSize;
            IQueryable<T> pageQuery = allItems.Skip(itemIndex).Take(pageSize);
            childSet.AddRange(await Task.Run(() => pageQuery.ToList(), cancellationToken).ConfigureAwait(false));
            return new PagerAsync<T>(childSet, pageIndex, pageSize, totalItemCount);
        }

        /// <summary>
        /// Async creates a child set of this collection of objects that can be individually accessed by index and containing metadata about the collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        /// <remarks>
        /// Deprecated: this only wraps <c>ToList()</c> in <c>Task.Run</c> and will be removed in v10. Use your provider's own
        /// <c>ToListAsync</c> (for example Entity Framework Core's), or the <c>ToPagerListAsync</c> overload that takes countAsync/toListAsync.
        /// </remarks>
        public static async Task<List<T>> ToListAsync<T>(this IEnumerable<T> allItems, CancellationToken cancellationToken)
        {
            return await Task.Run(() => allItems.ToList(), cancellationToken);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. By default 1.</param>
        /// <param name="pageSize">Size of individual child. By default 10.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static IPager<T> ToPagerList<T>(this IEnumerable<T> allItems, int pageIndex = 1, int pageSize = 10)
        {
            return allItems is IQueryable<T> query ? query.AsPagerList(pageIndex, pageSize) : PageInMemory(allItems, pageIndex, pageSize);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. By default 1.</param>
        /// <param name="pageSize">Size of individual child. By default 10.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static IPager<T> ToPagerList<T>(this IQueryable<T> allItems, int pageIndex = 1, int pageSize = 10)
        {
            return allItems.AsPagerList(pageIndex, pageSize);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. By default 1.</param>
        /// <param name="pageSize">Size of individual child. By default 10.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static async Task<IPager<T>> ToPagerListAsync<T>(this IEnumerable<T> allItems, int? pageIndex = 1, int pageSize = 10)
        {
            return allItems is IQueryable<T> query
                ? await AsPagerListAsync(query, pageIndex ?? 1, pageSize, CancellationToken.None)
                : PageInMemory(allItems, pageIndex ?? 1, pageSize);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. By default 1.</param>
        /// <param name="pageSize">Size of individual child. By default 10.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static async Task<IPager<T>> ToPagerListAsync<T>(this IEnumerable<T> allItems, int? pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return allItems is IQueryable<T> query
                ? await AsPagerListAsync(query, pageIndex ?? 1, pageSize, cancellationToken)
                : PageInMemory(allItems, pageIndex ?? 1, pageSize);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. By default 1.</param>
        /// <param name="pageSize">Size of individual child. By default 10.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static async Task<IPager<T>> ToPagerListAsync<T>(this IQueryable<T> allItems, int? pageIndex = 1, int pageSize = 10)
        {
            return await AsPagerListAsync(allItems, pageIndex ?? 1, pageSize, CancellationToken.None);
        }

        /// <summary>
        /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. By default 1.</param>
        /// <param name="pageSize">Size of individual child.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static async Task<IPager<T>> ToPagerListAsync<T>(this IQueryable<T> allItems, int? pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            return await AsPagerListAsync(allItems, pageIndex ?? 1, pageSize, cancellationToken);
        }

        /// <summary>
        /// Truly asynchronous paging using the query provider's own async operators (for example Entity Framework Core's
        /// <c>CountAsync</c> and <c>ToListAsync</c>), without P.Pager taking a dependency on that provider.
        /// </summary>
        /// <example>
        /// <code>
        /// var page = await db.Members.OrderBy(m => m.Id).ToPagerListAsync(pageIndex, 20,
        ///     (q, ct) => q.CountAsync(ct),
        ///     (q, ct) => q.ToListAsync(ct),
        ///     cancellationToken);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. Values below 1 return the first page; values past the end return the last page.</param>
        /// <param name="pageSize">Size of individual child. Must be at least 1.</param>
        /// <param name="countAsync">Asynchronously counts the items in the query.</param>
        /// <param name="toListAsync">Asynchronously materializes the query.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static async Task<IPager<T>> ToPagerListAsync<T>(
            this IQueryable<T> allItems,
            int pageIndex,
            int pageSize,
            Func<IQueryable<T>, CancellationToken, Task<int>> countAsync,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync,
            CancellationToken cancellationToken = default)
        {
            if (allItems == null) throw new ArgumentNullException(nameof(allItems));
            if (countAsync == null) throw new ArgumentNullException(nameof(countAsync));
            if (toListAsync == null) throw new ArgumentNullException(nameof(toListAsync));
            ValidatePageSize(pageSize);
            CheckOrdering(allItems);

            int totalItemCount = await countAsync(allItems, cancellationToken).ConfigureAwait(false);
            pageIndex = ClampPageIndex(pageIndex, pageSize, totalItemCount);
            int itemIndex = (pageIndex - 1) * pageSize;
            List<T> items = await toListAsync(allItems.Skip(itemIndex).Take(pageSize), cancellationToken).ConfigureAwait(false);
            return new PagerAsync<T>(items, pageIndex, pageSize, totalItemCount);
        }

        /// <summary>
        /// Pages a query when the total number of items is already known (for example cached), so no COUNT query is run.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. Values below 1 return the first page; values past the end return the last page.</param>
        /// <param name="pageSize">Size of individual child. Must be at least 1.</param>
        /// <param name="totalItemCount">The known total number of items in <paramref name="allItems"/>.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static IPager<T> ToPagerList<T>(this IQueryable<T> allItems, int pageIndex, int pageSize, int totalItemCount)
        {
            if (allItems == null) throw new ArgumentNullException(nameof(allItems));
            ValidatePageSize(pageSize);
            ValidateTotal(totalItemCount);
            CheckOrdering(allItems);
            pageIndex = ClampPageIndex(pageIndex, pageSize, totalItemCount);
            return new Pager<T>(allItems.Skip((pageIndex - 1) * pageSize).Take(pageSize), pageIndex, pageSize, totalItemCount);
        }

        /// <summary>
        /// Asynchronously pages a query when the total number of items is already known (for example cached), so no COUNT query is run.
        /// </summary>
        /// <typeparam name="T">The type of object the collection should contain.</typeparam>
        /// <param name="allItems">Parent that is divided in to children.</param>
        /// <param name="pageIndex">Index of child set within the parent list. Values below 1 return the first page; values past the end return the last page.</param>
        /// <param name="pageSize">Size of individual child. Must be at least 1.</param>
        /// <param name="totalItemCount">The known total number of items in <paramref name="allItems"/>.</param>
        /// <param name="toListAsync">Asynchronously materializes the query, for example <c>(q, ct) =&gt; q.ToListAsync(ct)</c>.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Child set that is divided from parent list.</returns>
        public static async Task<IPager<T>> ToPagerListAsync<T>(
            this IQueryable<T> allItems,
            int pageIndex,
            int pageSize,
            int totalItemCount,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync,
            CancellationToken cancellationToken = default)
        {
            if (allItems == null) throw new ArgumentNullException(nameof(allItems));
            if (toListAsync == null) throw new ArgumentNullException(nameof(toListAsync));
            ValidatePageSize(pageSize);
            ValidateTotal(totalItemCount);
            CheckOrdering(allItems);
            pageIndex = ClampPageIndex(pageIndex, pageSize, totalItemCount);
            List<T> items = await toListAsync(allItems.Skip((pageIndex - 1) * pageSize).Take(pageSize), cancellationToken).ConfigureAwait(false);
            return new PagerAsync<T>(items, pageIndex, pageSize, totalItemCount);
        }

        private static void ValidateTotal(int totalItemCount)
        {
            if (totalItemCount < 0)
                throw new ArgumentOutOfRangeException(nameof(totalItemCount), totalItemCount, "totalItemCount cannot be negative.");
        }
    }
}
