using System.Collections.Generic;
using System.Linq;

namespace P.Pager
{
    /// <summary>
    /// Serializable paging metadata (no items), suitable for JSON responses or a pagination header.
    /// </summary>
    public sealed class PagerMetadata
    {
        /// <summary>Index (1-based) of the current page.</summary>
        public int CurrentPageIndex { get; set; }

        /// <summary>Maximum number of items per page.</summary>
        public int PageSize { get; set; }

        /// <summary>Total number of items in the source.</summary>
        public int TotalItemCount { get; set; }

        /// <summary>Total number of pages.</summary>
        public int TotalPageCount { get; set; }

        /// <summary>True if there is a page before the current one.</summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>True if there is a page after the current one.</summary>
        public bool HasNextPage { get; set; }

        /// <summary>1-based index of the first item on this page (0 when empty).</summary>
        public int StartItemIndex { get; set; }

        /// <summary>1-based index of the last item on this page (0 when empty).</summary>
        public int EndItemIndex { get; set; }
    }

    /// <summary>
    /// A page of items plus its <see cref="PagerMetadata"/>, shaped for JSON APIs and Blazor/SPA clients.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    public sealed class PagedResult<T>
    {
        /// <summary>The items on the current page.</summary>
        public IReadOnlyList<T> Items { get; set; } = [];

        /// <summary>Paging metadata.</summary>
        public PagerMetadata Metadata { get; set; } = new PagerMetadata();
    }

    /// <summary>
    /// Conversions from <see cref="IPager"/> to serializable shapes.
    /// </summary>
    public static class PagedResultExtensions
    {
        /// <summary>
        /// Copies the paging metadata of <paramref name="pager"/> into a serializable <see cref="PagerMetadata"/>.
        /// </summary>
        /// <param name="pager">The pager.</param>
        /// <returns>The metadata.</returns>
        public static PagerMetadata ToMetadata(this IPager pager) => new()
        {
            CurrentPageIndex = pager.CurrentPageIndex,
            PageSize = pager.PageSize,
            TotalItemCount = pager.TotalItemCount,
            TotalPageCount = pager.TotalPageCount,
            HasPreviousPage = pager.HasPreviousPage,
            HasNextPage = pager.HasNextPage,
            StartItemIndex = pager.StartItemIndex,
            EndItemIndex = pager.EndItemIndex,
        };

        /// <summary>
        /// Converts <paramref name="pager"/> into a JSON-friendly <see cref="PagedResult{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items.</typeparam>
        /// <param name="pager">The pager.</param>
        /// <returns>The items and metadata.</returns>
        public static PagedResult<T> ToPagedResult<T>(this IPager<T> pager) => new()
        {
            Items = pager.ToList(),
            Metadata = pager.ToMetadata(),
        };
    }
}
