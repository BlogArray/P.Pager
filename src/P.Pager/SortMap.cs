using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace P.Pager
{
    /// <summary>
    /// The sortable fields of <typeparamref name="T"/>: an allow-list that maps public sort names to key selectors.
    /// Only listed fields can be sorted on, so a sort string from the query string can't reach other columns.
    /// Build it once and reuse it (it is safe to share between requests after it is built).
    /// </summary>
    /// <example>
    /// <code>
    /// static readonly SortMap&lt;Member&gt; MemberSorts = new SortMap&lt;Member&gt;()
    ///     .Add("name", m =&gt; m.Name)
    ///     .Add("created", m =&gt; m.Created)
    ///     .Default("-created")
    ///     .ThenBy(m =&gt; m.Id); // unique tiebreaker so pages never repeat or skip rows
    /// </code>
    /// </example>
    /// <typeparam name="T">The type being sorted.</typeparam>
    public sealed class SortMap<T>
    {
        private readonly Dictionary<string, Func<IQueryable<T>, bool, bool, IQueryable<T>>> _fields = new(StringComparer.OrdinalIgnoreCase);
        private Func<IQueryable<T>, IQueryable<T>>? _tiebreaker;

        /// <summary>The sort used when the request has no valid sort field. Null for none.</summary>
        public string? DefaultSort { get; private set; }

        /// <summary>The most sort fields applied from one sort string (extra fields are ignored). By default 3.</summary>
        public int MaxFields { get; set; } = 3;

        /// <summary>The allowed sort names.</summary>
        public IEnumerable<string> Names => _fields.Keys;

        /// <summary>Allows sorting by <paramref name="keySelector"/> under the public name <paramref name="name"/>.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="name">The name used in sort strings (case-insensitive), for example created.</param>
        /// <param name="keySelector">The key, for example <c>m =&gt; m.Created</c>.</param>
        /// <returns>This map, for chaining.</returns>
        public SortMap<T> Add<TKey>(string name, Expression<Func<T, TKey>> keySelector)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A sort name is required.", nameof(name));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (name.StartsWith("-", StringComparison.Ordinal) || name.StartsWith("+", StringComparison.Ordinal) || name.Contains(","))
                throw new ArgumentException("Sort names can't start with + or - or contain commas.", nameof(name));

            _fields[name.Trim()] = (query, first, descending) =>
                first
                    ? (descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector))
                    : (descending ? ((IOrderedQueryable<T>)query).ThenByDescending(keySelector) : ((IOrderedQueryable<T>)query).ThenBy(keySelector));
            return this;
        }

        /// <summary>The sort to use when the request has no valid sort field, for example <c>-created</c>.</summary>
        /// <param name="sort">A sort string made of names added with <see cref="Add"/>.</param>
        /// <returns>This map, for chaining.</returns>
        public SortMap<T> Default(string sort)
        {
            DefaultSort = sort;
            return this;
        }

        /// <summary>
        /// A unique key always applied last (for example the primary key), so rows with equal sort values keep a stable order
        /// and pages never repeat or skip items. Also used as the only order when there is no sort at all.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="keySelector">The unique key, for example <c>m =&gt; m.Id</c>.</param>
        /// <param name="descending">True to order the tiebreaker descending.</param>
        /// <returns>This map, for chaining.</returns>
        public SortMap<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector, bool descending = false)
        {
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            _tiebreaker = query => query is IOrderedQueryable<T> ordered && IsOrdered(query)
                ? (descending ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector))
                : (descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector));
            return this;
        }

        internal IQueryable<T> Apply(IQueryable<T> query, string? sort)
        {
            bool applied = false;
            if (!string.IsNullOrWhiteSpace(sort))
                query = ApplyFields(query, sort!, ref applied);
            if (!applied && !string.IsNullOrWhiteSpace(DefaultSort))
                query = ApplyFields(query, DefaultSort!, ref applied);
            return _tiebreaker != null ? _tiebreaker(query) : query;
        }

        private IQueryable<T> ApplyFields(IQueryable<T> query, string sort, ref bool applied)
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in sort.Split(','))
            {
                if (seen.Count >= MaxFields)
                    break;

                string part = raw.Trim();
                bool descending = part.StartsWith("-", StringComparison.Ordinal);
                if (descending || part.StartsWith("+", StringComparison.Ordinal))
                    part = part.Substring(1).Trim();

                if (part.Length == 0 || !seen.Add(part) || !_fields.TryGetValue(part, out Func<IQueryable<T>, bool, bool, IQueryable<T>>? apply))
                    continue; // unknown or repeated fields are ignored

                query = apply(query, !applied, descending);
                applied = true;
            }
            return query;
        }

        private static bool IsOrdered(IQueryable<T> query) =>
            query.Expression is MethodCallExpression call && call.Method.DeclaringType == typeof(Queryable) &&
            (call.Method.Name.StartsWith("OrderBy", StringComparison.Ordinal) || call.Method.Name.StartsWith("ThenBy", StringComparison.Ordinal));
    }

    /// <summary>
    /// Safe sorting from untrusted sort strings.
    /// </summary>
    public static class SortExtension
    {
        /// <summary>
        /// Sorts by a comma-separated sort string such as <c>name,-created</c> (a leading - means descending).
        /// Only names in <paramref name="sortMap"/> are used; unknown names are ignored. When nothing valid is requested
        /// the map's default sort is used, and its tiebreaker is always applied last.
        /// </summary>
        /// <typeparam name="T">The type being sorted.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="sort">The sort string, usually from the query string. May be null.</param>
        /// <param name="sortMap">The allowed sort fields.</param>
        /// <returns>The sorted query.</returns>
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string? sort, SortMap<T> sortMap)
        {
            return source == null
                ? throw new ArgumentNullException(nameof(source))
                : sortMap == null ? throw new ArgumentNullException(nameof(sortMap)) : sortMap.Apply(source, sort);
        }
    }
}
