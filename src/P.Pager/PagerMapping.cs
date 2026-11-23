using System;
using System.Linq;

namespace P.Pager
{
    /// <summary>
    /// Converts the items of a page while keeping its paging metadata.
    /// </summary>
    public static class PagerMapping
    {
        /// <summary>
        /// Converts each item on the page (for example entity to DTO) and keeps the paging metadata.
        /// The conversion runs in memory on the current page only. To shape data in the database instead, call
        /// <c>Select</c> on the query before paging.
        /// </summary>
        /// <example>
        /// <code>
        /// IPager&lt;MemberDto&gt; dtos = db.Members.OrderBy(m =&gt; m.Id).ToPagerList(page, 20).Map(m =&gt; new MemberDto(m.Id, m.Name));
        /// </code>
        /// </example>
        /// <typeparam name="TSource">The type of the items on the page.</typeparam>
        /// <typeparam name="TResult">The type to convert to.</typeparam>
        /// <param name="pager">The page to convert.</param>
        /// <param name="selector">The conversion.</param>
        /// <returns>A page with the converted items and the same metadata.</returns>
        public static IPager<TResult> Map<TSource, TResult>(this IPager<TSource> pager, Func<TSource, TResult> selector)
        {
            return pager == null
                ? throw new ArgumentNullException(nameof(pager))
                : selector == null
                ? throw new ArgumentNullException(nameof(selector))
                : (IPager<TResult>)new PagerAsync<TResult>(pager.Select(selector), pager.CurrentPageIndex, pager.PageSize, pager.TotalItemCount);
        }

        /// <summary>
        /// Converts each item on the page (for example entity to DTO) and keeps the paging metadata.
        /// </summary>
        /// <typeparam name="TSource">The type of the items on the page.</typeparam>
        /// <typeparam name="TResult">The type to convert to.</typeparam>
        /// <param name="slice">The page to convert.</param>
        /// <param name="selector">The conversion.</param>
        /// <returns>A page with the converted items and the same metadata.</returns>
        public static ISlice<TResult> Map<TSource, TResult>(this ISlice<TSource> slice, Func<TSource, TResult> selector)
        {
            return slice == null
                ? throw new ArgumentNullException(nameof(slice))
                : selector == null
                ? throw new ArgumentNullException(nameof(selector))
                : (ISlice<TResult>)new Slice<TResult>(slice.Select(selector).ToList(), slice.CurrentPageIndex, slice.PageSize, slice.HasNextPage);
        }
    }
}
