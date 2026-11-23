using Microsoft.AspNetCore.Components;

namespace P.Pager.Blazor
{
    /// <summary>
    /// A pagination control for an <see cref="IPager"/> or <see cref="ISlice"/>. Uses <see cref="PagerSettings.DefaultTheme"/> class names unless <see cref="Theme"/> is set.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;Pagination Pager="pager" OnPageChanged="LoadPage" /&gt;
    /// &lt;Pagination Pager="pager" PageUrl="@(p =&gt; $"/members?page={p}")" /&gt;
    /// </code>
    /// </example>
    public partial class Pagination
    {
        /// <summary>The pager to render. Nothing is rendered when both this and <see cref="Slice"/> are null.</summary>
        [Parameter] public IPager? Pager { get; set; }

        /// <summary>
        /// A count-free slice to render instead of <see cref="Pager"/>. Only First, Previous and Next are shown,
        /// because page numbers and Last need the total.
        /// </summary>
        [Parameter] public ISlice? Slice { get; set; }

        /// <summary>Raised with the target page number when a page link or button is clicked.</summary>
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }

        /// <summary>
        /// When set, pages are rendered as links to these URLs (works with static SSR and enhanced navigation);
        /// otherwise they are rendered as buttons that raise <see cref="OnPageChanged"/>.
        /// </summary>
        [Parameter] public Func<int, string>? PageUrl { get; set; }

        /// <summary>Maximum number of page numbers to show. By default 5.</summary>
        [Parameter] public int PagesToDisplay { get; set; } = 5;

        /// <summary>Show page numbers. By default true.</summary>
        [Parameter] public bool ShowPageNumbers { get; set; } = true;

        /// <summary>Show First and Last. By default true.</summary>
        [Parameter] public bool ShowFirstLast { get; set; } = true;

        /// <summary>Show Previous and Next. By default true.</summary>
        [Parameter] public bool ShowPreviousNext { get; set; } = true;

        /// <summary>Render nothing when there is only one page. By default false.</summary>
        [Parameter] public bool HideWhenSinglePage { get; set; }

        /// <summary>Text of the First link. By default «.</summary>
        [Parameter] public string FirstText { get; set; } = "«";

        /// <summary>Text of the Previous link. By default ‹.</summary>
        [Parameter] public string PreviousText { get; set; } = "‹";

        /// <summary>Text of the Next link. By default ›.</summary>
        [Parameter] public string NextText { get; set; } = "›";

        /// <summary>Text of the Last link. By default ».</summary>
        [Parameter] public string LastText { get; set; } = "»";

        /// <summary>aria-label of the navigation landmark. By default Pagination.</summary>
        [Parameter] public string AriaLabel { get; set; } = "Pagination";

        /// <summary>
        /// The CSS class names to render with, for example <see cref="PagerTheme.Bootstrap"/>. When null,
        /// <see cref="PagerSettings.DefaultTheme"/> is used if the app set it, otherwise <see cref="PagerTheme.Neutral"/>
        /// (framework-neutral classes styled by <c>_content/P.Pager.Blazor/pager.css</c>).
        /// The *Class parameters below override single classes of the theme.
        /// </summary>
        [Parameter] public PagerTheme? Theme { get; set; }

        /// <summary>Css class of the &lt;nav&gt;. Overrides the theme.</summary>
        [Parameter] public string? ContainerClass { get; set; }

        /// <summary>Css class of the &lt;ul&gt;. Overrides the theme.</summary>
        [Parameter] public string? ListClass { get; set; }

        /// <summary>Css class of each &lt;li&gt;. Overrides the theme.</summary>
        [Parameter] public string? ItemClass { get; set; }

        /// <summary>Css class of each link, button or span. Overrides the theme.</summary>
        [Parameter] public string? LinkClass { get; set; }

        /// <summary>Css class added to the current page's &lt;li&gt;. Overrides the theme.</summary>
        [Parameter] public string? ActiveClass { get; set; }

        /// <summary>Css class added to the current page's link. Overrides the theme.</summary>
        [Parameter] public string? ActiveLinkClass { get; set; }

        /// <summary>Css class added to disabled &lt;li&gt; elements. Overrides the theme.</summary>
        [Parameter] public string? DisabledClass { get; set; }

        /// <summary>Css class added to disabled links. Overrides the theme.</summary>
        [Parameter] public string? DisabledLinkClass { get; set; }

        // A new component has no legacy markup to keep: Neutral unless the app chose a theme.
        private PagerTheme EffectiveTheme => Theme ?? (PagerSettings.IsDefaultThemeSet ? PagerSettings.DefaultTheme : PagerTheme.Neutral);
        private string? CssContainer => ContainerClass ?? EffectiveTheme.Container;
        private string? CssList => ListClass ?? EffectiveTheme.List;
        private string? CssItem => ItemClass ?? EffectiveTheme.Item;
        private string? CssLink => LinkClass ?? EffectiveTheme.Link;
        private string? CssActiveItem => ActiveClass ?? EffectiveTheme.ActiveItem;
        private string? CssActiveLink => ActiveLinkClass ?? EffectiveTheme.ActiveLink;
        private string? CssDisabledItem => DisabledClass ?? EffectiveTheme.DisabledItem;
        private string? CssDisabledLink => DisabledLinkClass ?? EffectiveTheme.DisabledLink;

        private IPager? Source => Pager ?? (Slice is null ? null : new SliceView(Slice));

        private (int First, int Last) Window()
        {
            IPager pager = Source!;
            int total = pager.TotalPageCount;
            int size = Math.Max(1, PagesToDisplay);
            if (total <= size)
                return (1, total);

            int first = Math.Max(1, pager.CurrentPageIndex - size / 2);
            int last = first + size - 1;
            if (last > total)
            {
                last = total;
                first = total - size + 1;
            }
            return (first, last);
        }

        /// <summary>A slice seen as a pager whose last known page is the next one. Only First/Previous/Next use it.</summary>
        private sealed class SliceView(ISlice slice) : IPager
        {
            public int CurrentPageIndex { get => slice.CurrentPageIndex; set => throw new NotSupportedException(); }
            public int PageSize { get => slice.PageSize; set => throw new NotSupportedException(); }
            public int TotalItemCount { get => slice.EndItemIndex; set => throw new NotSupportedException(); }
            public int TotalPageCount => slice.HasNextPage ? slice.CurrentPageIndex + 1 : slice.CurrentPageIndex;
            public bool HasPreviousPage => slice.HasPreviousPage;
            public bool HasNextPage => slice.HasNextPage;
            public bool IsFirstPage => slice.CurrentPageIndex == 1;
            public bool IsLastPage => !slice.HasNextPage;
            public int StartItemIndex => slice.StartItemIndex;
            public int EndItemIndex => slice.EndItemIndex;
        }

        private static string? Join(string? a, string? b) =>
            string.IsNullOrWhiteSpace(a) ? b : string.IsNullOrWhiteSpace(b) ? a : a + " " + b;
    }
}
