using System;
using System.Collections;
using System.Collections.Generic;

namespace P.Pager
{
    /// <summary>
    /// Represents a child set of a collection of objects that can be individually accessed by index and containing metadata about the parent collection of objects.
    /// </summary>
    /// <typeparam name="T">The type of object the collection should contain.</typeparam>
    public abstract class BasePager<T> : IPager<T>
    {
        /// <summary>
        /// Appending current child to this variable. 
        /// </summary>
        protected readonly List<T> Childset = [];

        /// <summary>
        /// Parameterless constructor.
        /// </summary>
        protected internal BasePager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pager{T}"/> class that divides the supplied parent into childs the size of the supplied pageSize.
        /// </summary>
        /// <param name="pageIndex">Index of child set within the parent list.</param>
        /// <param name="pageSize">Size of individual child set.</param>
        /// <param name="totalItemCount">Total number of items in parent list.</param>
        protected internal BasePager(int pageIndex, int pageSize, int totalItemCount)
        {
            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex), pageIndex, "pageIndex cannot be less than 1.");
            }

            if (pageSize < 0) // 0 is allowed, as in 9.0 (an empty page)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "pageSize cannot be negative.");
            }

            TotalItemCount = totalItemCount;
            CurrentPageIndex = pageIndex;
            PageSize = pageSize;
        }

        /// <summary>
        /// Index of child set within the parent list.
        /// </summary>
        /// <remarks>Deprecated setter: changing paging metadata after construction makes it inconsistent with the items. It will be removed in v10.</remarks>
        public int CurrentPageIndex { get; set; }

        /// <summary>
        /// Size of individual child set.
        /// </summary>
        /// <remarks>Deprecated setter: changing paging metadata after construction makes it inconsistent with the items. It will be removed in v10.</remarks>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items in parent list.
        /// </summary>
        /// <remarks>Deprecated setter: changing paging metadata after construction makes it inconsistent with the items. It will be removed in v10.</remarks>
        public int TotalItemCount { get; set; }

        /// <summary>
        /// Total number of child sets in parent list.
        /// </summary>
        public int TotalPageCount { get { return PageSize > 0 ? (int)((TotalItemCount + (long)PageSize - 1) / PageSize) : 0; } }

        /// <summary>
        /// Returns true if this is not first child.
        /// </summary>
        public bool HasPreviousPage { get { return CurrentPageIndex > 1; } }

        /// <summary>
        /// Returns true if this is not the last child.
        /// </summary>
        public bool HasNextPage { get { return CurrentPageIndex < TotalPageCount; } }

        /// <summary>
        /// Returns true if this is the first child.
        /// </summary>
        public bool IsFirstPage { get { return CurrentPageIndex == 1; } }

        /// <summary>
        /// Returns true if this is the last child.
        /// </summary>
        public bool IsLastPage { get { return CurrentPageIndex >= TotalPageCount; } }

        /// <summary>
        /// Sub-index of first item in paged child. 0 when there are no items.
        /// </summary>
        public int StartItemIndex { get { return TotalItemCount == 0 ? 0 : ((CurrentPageIndex - 1) * PageSize) + 1; } }

        /// <summary>
        /// Sub-index of last item in paged child.
        /// </summary>
        public int EndItemIndex { get { return TotalItemCount > CurrentPageIndex * PageSize ? CurrentPageIndex * PageSize : TotalItemCount; } }

        /// <summary>
        /// Returns an enumerator that iterates through the BasePager&lt;T&gt;.
        /// </summary>
        /// <returns>A BasePaget&lt;T&gt;.Enumerator for the BasePager&lt;T&gt;.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return Childset.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the BasePager&lt;T&gt;.
        /// </summary>
        /// <returns>A BasePaget&lt;T&gt;.Enumerator for the BasePager&lt;T&gt;.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        public T this[int index] => Childset[index];
    }
}
