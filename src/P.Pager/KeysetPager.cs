using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace P.Pager
{
    /// <summary>
    /// One page of a keyset ("seek" / cursor) paged query. Unlike offset paging, keyset paging stays fast on deep
    /// pages because the database seeks directly to the key instead of skipping rows. It has no total count or page numbers.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <typeparam name="TKey">The type of the unique, ordered key.</typeparam>
    public sealed class KeysetPage<T, TKey>
    {
        /// <summary>The items on this page.</summary>
        public IReadOnlyList<T> Items { get; set; } = [];

        /// <summary>The page size that was requested.</summary>
        public int PageSize { get; set; }

        /// <summary>True if more items exist after this page.</summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// The key of the last item on this page. Pass it as <c>after</c> to get the next page. Default when the page is empty.
        /// </summary>
        public TKey? NextKey { get; set; }
    }

    /// <summary>
    /// Keyset (seek) paging extensions.
    /// </summary>
    public static class KeysetPagerExtension
    {
        /// <summary>
        /// Returns the first page, ordered by <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <typeparam name="TKey">The type of the key. It must be unique per item (for example a primary key).</typeparam>
        /// <param name="source">The source query. Don't order it yourself; ordering is applied by the key.</param>
        /// <param name="keySelector">Selects the unique key, for example <c>x =&gt; x.Id</c>.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <param name="descending">True to page from the highest key to the lowest.</param>
        /// <returns>The page.</returns>
        public static KeysetPage<T, TKey> ToKeysetPage<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, int pageSize, bool descending = false)
            where TKey : IComparable<TKey>
            => CreatePage(BuildQuery(source, keySelector, default, false, pageSize, descending).ToList(), keySelector, pageSize);

        /// <summary>
        /// Returns the page of items whose key comes after <paramref name="after"/>, ordered by <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <typeparam name="TKey">The type of the key. It must be unique per item (for example a primary key).</typeparam>
        /// <param name="source">The source query. Don't order it yourself; ordering is applied by the key.</param>
        /// <param name="keySelector">Selects the unique key, for example <c>x =&gt; x.Id</c>.</param>
        /// <param name="after">The <see cref="KeysetPage{T, TKey}.NextKey"/> of the previous page.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <param name="descending">True to page from the highest key to the lowest.</param>
        /// <returns>The page.</returns>
        public static KeysetPage<T, TKey> ToKeysetPage<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, TKey after, int pageSize, bool descending = false)
            where TKey : IComparable<TKey>
            => CreatePage(BuildQuery(source, keySelector, after, true, pageSize, descending).ToList(), keySelector, pageSize);

        /// <summary>
        /// Asynchronously returns the first page using the provider's own async operator,
        /// for example <c>(q, ct) =&gt; q.ToListAsync(ct)</c> with Entity Framework Core.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <typeparam name="TKey">The type of the key. It must be unique per item (for example a primary key).</typeparam>
        /// <param name="source">The source query. Don't order it yourself; ordering is applied by the key.</param>
        /// <param name="keySelector">Selects the unique key, for example <c>x =&gt; x.Id</c>.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <param name="toListAsync">Asynchronously materializes the query.</param>
        /// <param name="descending">True to page from the highest key to the lowest.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>The page.</returns>
        public static Task<KeysetPage<T, TKey>> ToKeysetPageAsync<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, int pageSize,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync, bool descending = false, CancellationToken cancellationToken = default)
            where TKey : IComparable<TKey>
            => ToKeysetPageCoreAsync(source, keySelector, default, false, pageSize, toListAsync, descending, cancellationToken);

        /// <summary>
        /// Asynchronously returns the page of items whose key comes after <paramref name="after"/> using the provider's own
        /// async operator, for example <c>(q, ct) =&gt; q.ToListAsync(ct)</c> with Entity Framework Core.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <typeparam name="TKey">The type of the key. It must be unique per item (for example a primary key).</typeparam>
        /// <param name="source">The source query. Don't order it yourself; ordering is applied by the key.</param>
        /// <param name="keySelector">Selects the unique key, for example <c>x =&gt; x.Id</c>.</param>
        /// <param name="after">The <see cref="KeysetPage{T, TKey}.NextKey"/> of the previous page.</param>
        /// <param name="pageSize">Maximum number of items. Must be at least 1.</param>
        /// <param name="toListAsync">Asynchronously materializes the query.</param>
        /// <param name="descending">True to page from the highest key to the lowest.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>The page.</returns>
        public static Task<KeysetPage<T, TKey>> ToKeysetPageAsync<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, TKey after, int pageSize,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync, bool descending = false, CancellationToken cancellationToken = default)
            where TKey : IComparable<TKey>
            => ToKeysetPageCoreAsync(source, keySelector, after, true, pageSize, toListAsync, descending, cancellationToken);

        private static async Task<KeysetPage<T, TKey>> ToKeysetPageCoreAsync<T, TKey>(IQueryable<T> source, Expression<Func<T, TKey>> keySelector, TKey? after, bool hasAfter, int pageSize,
            Func<IQueryable<T>, CancellationToken, Task<List<T>>> toListAsync, bool descending, CancellationToken cancellationToken)
            where TKey : IComparable<TKey>
        {
            if (toListAsync == null) throw new ArgumentNullException(nameof(toListAsync));
            IQueryable<T> query = BuildQuery(source, keySelector, after, hasAfter, pageSize, descending);
            List<T> items = await toListAsync(query, cancellationToken).ConfigureAwait(false);
            return CreatePage(items, keySelector, pageSize);
        }

        private static IQueryable<T> BuildQuery<T, TKey>(IQueryable<T> source, Expression<Func<T, TKey>> keySelector, TKey? after, bool hasAfter, int pageSize, bool descending)
            where TKey : IComparable<TKey>
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "pageSize cannot be less than 1.");

            if (hasAfter)
                source = source.Where(BuildSeekPredicate(keySelector, after, descending));

            source = descending ? source.OrderByDescending(keySelector) : source.OrderBy(keySelector);
            return source.Take(pageSize + 1); // one extra row tells us whether there is a next page
        }

        private static KeysetPage<T, TKey> CreatePage<T, TKey>(List<T> items, Expression<Func<T, TKey>> keySelector, int pageSize)
        {
            bool hasNext = items.Count > pageSize;
            if (hasNext)
                items.RemoveAt(items.Count - 1);

            return new KeysetPage<T, TKey>
            {
                Items = items,
                PageSize = pageSize,
                HasNextPage = hasNext,
                NextKey = items.Count > 0 ? keySelector.Compile()(items[items.Count - 1]) : default,
            };
        }

        /// <summary>
        /// Builds <c>x =&gt; key(x) &gt; after</c> (or &lt; when descending). Uses the &gt; operator for numbers and date/time types,
        /// otherwise <c>key(x).CompareTo(after) &gt; 0</c> (strings, Guid, ...), which EF Core translates.
        /// No reflection is used, so this is trimming and Native AOT safe.
        /// </summary>
        private static Expression<Func<T, bool>> BuildSeekPredicate<T, TKey>(Expression<Func<T, TKey>> keySelector, TKey? after, bool descending)
            where TKey : IComparable<TKey>
        {
            Expression key = keySelector.Body;

            // A closure capture, exactly like `x => x.Id > after` written by hand, so providers send it as a query parameter.
            Expression<Func<TKey?>> captured = () => after;
            Expression value = captured.Body;
            if (value.Type != key.Type)
                value = Expression.Convert(value, key.Type);

            Expression comparison;
            if (HasComparisonOperator(key.Type))
            {
                comparison = descending ? Expression.LessThan(key, value) : Expression.GreaterThan(key, value);
            }
            else
            {
                Expression call = CompareTo<TKey>(key, value);
                Expression zero = Expression.Constant(0);
                comparison = descending ? Expression.LessThan(call, zero) : Expression.GreaterThan(call, zero);
            }

            return Expression.Lambda<Func<T, bool>>(comparison, keySelector.Parameters);
        }

        /// <summary>
        /// Builds <c>left.CompareTo(right)</c>. String and Guid use their own CompareTo methods (which EF Core recognizes);
        /// other types use <see cref="IComparable{T}.CompareTo"/>. The method is taken from compiler-built lambdas, not reflection.
        /// </summary>
        private static Expression CompareTo<TKey>(Expression left, Expression right)
            where TKey : IComparable<TKey>
        {
            LambdaExpression template;
            if (typeof(TKey) == typeof(string))
            {
                Expression<Func<string, string, int>> e = (a, b) => a.CompareTo(b);
                template = e;
            }
            else if (typeof(TKey) == typeof(Guid))
            {
                Expression<Func<Guid, Guid, int>> e = (a, b) => a.CompareTo(b);
                template = e;
            }
            else
            {
                Expression<Func<TKey, TKey, int>> e = (a, b) => a.CompareTo(b);
                template = e;
            }

            return new ParameterReplacer(template.Parameters[0], left, template.Parameters[1], right).Visit(template.Body);
        }

        private static bool HasComparisonOperator(Type type)
        {
            Type t = Nullable.GetUnderlyingType(type) ?? type;
            return t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)
                || t == typeof(uint) || t == typeof(ulong) || t == typeof(ushort) || t == typeof(sbyte)
                || t == typeof(float) || t == typeof(double) || t == typeof(decimal)
                || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan)
#if NET6_0_OR_GREATER
                || t == typeof(DateOnly) || t == typeof(TimeOnly)
#endif
                ;
        }

        private sealed class ParameterReplacer(ParameterExpression a, Expression aValue, ParameterExpression b, Expression bValue) : ExpressionVisitor
        {
            protected override Expression VisitParameter(ParameterExpression node) =>
                node == a ? aValue : node == b ? bValue : base.VisitParameter(node);
        }
    }
}
