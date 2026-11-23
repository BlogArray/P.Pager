namespace P.Pager
{
    /// <summary>
    /// The CSS class names a pager is rendered with. Used by P.Pager.Mvc.Core (<c>PagerOptions.ApplyTheme</c>) and
    /// P.Pager.Blazor (<c>&lt;Pagination Theme="..." /&gt;</c>), so one theme works everywhere.
    /// </summary>
    /// <remarks>
    /// <see cref="Classic"/> is the markup of P.Pager 9.0 and earlier and stays the MVC default in 9.x, so upgrading changes nothing.
    /// <see cref="Neutral"/> uses framework-neutral class names styled by the optional <c>pager.css</c> shipped in the UI packages
    /// (<c>_content/P.Pager.Mvc.Core/pager.css</c> or <c>_content/P.Pager.Blazor/pager.css</c>).
    /// Set <see cref="PagerSettings.DefaultTheme"/> once at startup to use another theme everywhere.
    /// </remarks>
    public sealed class PagerTheme
    {
        /// <summary>Class of the element that wraps the pager.</summary>
        public string? Container { get; set; }

        /// <summary>Class of the &lt;ul&gt;.</summary>
        public string? List { get; set; }

        /// <summary>Class of each &lt;li&gt;.</summary>
        public string? Item { get; set; }

        /// <summary>Class of each link, button or text element.</summary>
        public string? Link { get; set; }

        /// <summary>Class added to the current page's &lt;li&gt;.</summary>
        public string? ActiveItem { get; set; }

        /// <summary>Class added to the current page's link (for frameworks that style the link, like Bulma).</summary>
        public string? ActiveLink { get; set; }

        /// <summary>Class added to disabled &lt;li&gt; elements (unavailable links, text, ellipses).</summary>
        public string? DisabledItem { get; set; }

        /// <summary>Class added to disabled links.</summary>
        public string? DisabledLink { get; set; }

        /// <summary>
        /// Element used for text that is not a link (pager text, entries text, ellipses): <c>span</c> (semantic) or
        /// <c>a</c> without href (the markup of P.Pager 9.0 and earlier, kept by <see cref="Classic"/>). By default span.
        /// </summary>
        public string TextElement { get; set; } = "span";

        /// <summary>
        /// The exact markup of P.Pager 9.0 and earlier: Bootstrap classes inside <c>&lt;div class="container"&gt;</c>, with text
        /// items rendered as <c>&lt;a&gt;</c>. This is the MVC default in 9.x so existing apps don't change.
        /// New apps should prefer <see cref="Bootstrap"/> (no <c>container</c> class, which clashes with Bootstrap's layout) or <see cref="Neutral"/>.
        /// </summary>
        public static PagerTheme Classic => new()
        {
            Container = "container",
            List = "pagination",
            Item = "page-item",
            Link = "page-link",
            ActiveItem = "active",
            DisabledItem = "disabled",
            TextElement = "a",
        };

        /// <summary>
        /// Framework-neutral classes (<c>pager</c>, <c>pager-list</c>, <c>pager-item</c>, <c>pager-link</c>, <c>is-active</c>, <c>is-disabled</c>),
        /// styled by the optional <c>pager.css</c>. The default of P.Pager.Blazor.
        /// </summary>
        public static PagerTheme Neutral => new()
        {
            Container = "pager",
            List = "pager-list",
            Item = "pager-item",
            Link = "pager-link",
            ActiveItem = "is-active",
            DisabledItem = "is-disabled",
        };

        /// <summary>
        /// Bootstrap 4 and 5 (https://getbootstrap.com/docs/5.3/components/pagination/). Same classes as <see cref="Classic"/>,
        /// but without the <c>container</c> wrapper class and with semantic <c>&lt;span&gt;</c> text items.
        /// </summary>
        public static PagerTheme Bootstrap => new()
        {
            List = "pagination",
            Item = "page-item",
            Link = "page-link",
            ActiveItem = "active",
            DisabledItem = "disabled",
        };

        /// <summary>Bulma (https://bulma.io/documentation/components/pagination/).</summary>
        public static PagerTheme Bulma => new()
        {
            Container = "pagination",
            List = "pagination-list",
            Link = "pagination-link",
            ActiveLink = "is-current",
            DisabledLink = "is-disabled",
        };

        /// <summary>
        /// Tailwind CSS utility classes. Tailwind only generates classes it finds in your sources, so add these classes to a
        /// safelist (or copy them into your own theme).
        /// </summary>
        public static PagerTheme Tailwind => new()
        {
            Container = "flex justify-center",
            List = "inline-flex items-center -space-x-px text-sm",
            Link = "block px-3 py-2 leading-tight border border-gray-300 bg-white text-gray-600 hover:bg-gray-100 hover:text-gray-800",
            ActiveLink = "!bg-blue-600 !text-white !border-blue-600",
            DisabledItem = "pointer-events-none",
            DisabledLink = "opacity-50 cursor-not-allowed",
        };

        /// <summary>Foundation 6 (https://get.foundation/sites/docs/pagination.html).</summary>
        public static PagerTheme Foundation => new()
        {
            List = "pagination",
            ActiveItem = "current",
            DisabledItem = "disabled",
        };

        /// <summary>UIkit 3 (https://getuikit.com/docs/pagination).</summary>
        public static PagerTheme UIkit => new()
        {
            List = "uk-pagination",
            ActiveItem = "uk-active",
            DisabledItem = "uk-disabled",
        };

        /// <summary>Materialize (https://materializecss.com/pagination.html).</summary>
        public static PagerTheme Materialize => new()
        {
            List = "pagination",
            Item = "waves-effect",
            ActiveItem = "active",
            DisabledItem = "disabled",
        };

        /// <summary>Returns a copy, so a shared theme can be adjusted without changing the original.</summary>
        /// <returns>A copy of this theme.</returns>
        public PagerTheme Clone() => (PagerTheme)MemberwiseClone();
    }
}
