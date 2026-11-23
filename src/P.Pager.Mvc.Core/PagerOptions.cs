using Microsoft.Extensions.Localization;

namespace P.Pager.Mvc.Core
{
    /// <summary>
    /// Formatting options for the pager rendered by <see cref="HtmlHelper"/>.
    /// </summary>
    public class PagerOptions
    {
        /// <summary>
        /// Settings for a count-free <see cref="ISlice"/>: a copy of these options without page numbers, Last, pager text or entries text,
        /// which all need the total.
        /// </summary>
        internal PagerOptions ForSlice()
        {
            PagerOptions copy = (PagerOptions)MemberwiseClone();
            copy.HasIndividualPages = false;
            copy.DisplayLastPage = PagerDisplayMode.Never;
            copy.HasPagerText = false;
            copy.HasEntriesText = false;
            return copy;
        }

        /// <summary>
        /// Default options for rendering pagination.
        /// </summary>
        public PagerOptions()
        {
            DisplayFirstPage = PagerDisplayMode.IfNeeded;
            DisplayLastPage = PagerDisplayMode.IfNeeded;
            DisplayPreviousPage = PagerDisplayMode.IfNeeded;
            DisplayNextPage = PagerDisplayMode.IfNeeded;
            PagesToDisplay = 5;
            HasIndividualPages = true;
            TextToIndividualPages = "{0}";
            TextForDelimiter = null;
            HasEllipses = true;
            EllipsesFormat = "&#8230;";
            TextToFirstPage = "&lt;&lt;";
            TextToPreviousPage = "&lt;";
            TextToNextPage = "&gt;";
            TextToLastPage = "&gt;&gt;";
            HasPagerText = false;
            PagerTextFormat = "Page {0} of {1}.";
            HasEntriesText = false;
            EntriesTextFormat = "Showing {0} to {1} of {2} entries.";
            AriaLabel = "Pagination";
            ApplyTheme(PagerSettings.DefaultTheme);
        }

        /// <summary>
        /// Default options rendered with <paramref name="theme"/>'s CSS classes, for example <c>new PagerOptions(PagerTheme.Bootstrap) { PagesToDisplay = 7 }</c>.
        /// </summary>
        /// <param name="theme">The theme, such as <see cref="PagerTheme.Bootstrap"/>.</param>
        public PagerOptions(PagerTheme theme) : this()
        {
            ApplyTheme(theme);
        }

        /// <summary>
        /// Sets all CSS class options from <paramref name="theme"/>, for example <c>PrePagerOptions.Minimal.ApplyTheme(PagerTheme.Bootstrap)</c>.
        /// Layout options (what is shown) are not changed.
        /// </summary>
        /// <param name="theme">The theme, such as <see cref="PagerTheme.Bootstrap"/>.</param>
        /// <returns>These options, for chaining.</returns>
        public PagerOptions ApplyTheme(PagerTheme theme)
        {
            if (theme == null) throw new System.ArgumentNullException(nameof(theme));
            ClassToPagerContainer = theme.Container;
            ClassToUl = theme.List;
            ClassToLi = theme.Item;
            PageClass = theme.Link;
            ClassToActiveLi = theme.ActiveItem;
            ClassToActiveLink = theme.ActiveLink;
            ClassToDisabledLi = theme.DisabledItem;
            ClassToDisabledLink = theme.DisabledLink;
            TextElement = theme.TextElement;
            return this;
        }

        /// <summary>
        /// If set to Always, render a hyperlink to the first page in the list. If set to IfNeeded, render the hyperlink only when the first page isn't visible in the paging control.
        /// </summary>
        public PagerDisplayMode DisplayFirstPage { get; set; }

        /// <summary>
        /// If set to Always, render a hyperlink to the last page in the list. If set to IfNeeded, render the hyperlink only when the last page isn't visible in the paging control.
        /// </summary>
        public PagerDisplayMode DisplayLastPage { get; set; }

        /// <summary>
        /// If set to Always, render a hyperlink to the previous page of the list. If set to IfNeeded, render the hyperlink only when there is a previous page in the list.
        /// </summary>
        public PagerDisplayMode DisplayPreviousPage { get; set; }

        /// <summary>
        /// If set to Always, render a hyperlink to the next page of the list. If set to IfNeeded, render the hyperlink only when there is a next page in the list.
        /// </summary>
        public PagerDisplayMode DisplayNextPage { get; set; }

        /// <summary>
        /// How many page numbers to display in pagination, by default it is 5.
        /// </summary>
        public int? PagesToDisplay { get; set; }

        /// <summary>
        /// Displays all pages, by default it shows 5 pages.
        /// </summary>
        /// <example>
        /// &lt; 1 2 3 4 5 &gt;
        /// &lt; 4 5 6 7 8 &gt;
        /// </example>
        public bool HasIndividualPages { get; set; }

        /// <summary>
        /// A formatted text to show to show inside the hyperlink. Use {0} to refer page number, by default it is set to {0}
        /// </summary>
        /// <example>
        /// page-{0}
        /// page{0}
        /// </example>
        public string TextToIndividualPages { get; set; }

        /// <summary>
        /// This will appear between each page number. If null or white space, no delimeter will display.
        /// </summary>
        public string? TextForDelimiter { get; set; }

        /// <summary>
        /// Adds an ellipe when all page numbers are not displaying, by default it is true.
        /// </summary>
        public bool HasEllipses { get; set; }

        /// <summary>
        /// A formatted text shows when all pages are not displaying, by default it is &#8230;
        /// </summary>
        public string EllipsesFormat { get; set; }

        /// <summary>
        /// HTML to show for first page link ({0} = target page), by default it is set to &amp;lt;&amp;lt; (renders as &lt;&lt;).
        /// </summary>
        public string TextToFirstPage { get; set; }

        /// <summary>
        /// HTML to show for previous page link ({0} = target page), by default it is set to &amp;lt; (renders as &lt;).
        /// </summary>
        public string TextToPreviousPage { get; set; }

        /// <summary>
        /// HTML to show for next page link ({0} = target page), by default it is set to &amp;gt; (renders as &gt;).
        /// </summary>
        public string TextToNextPage { get; set; }

        /// <summary>
        /// HTML to show for last page link ({0} = target page), by default it is set to &amp;gt;&amp;gt; (renders as &gt;&gt;).
        /// </summary>
        public string TextToLastPage { get; set; }

        /// <summary>
        /// Css class to append to &lt;div&gt; element wrapping the pager, by default it is set to container (from PagerSettings.DefaultTheme, PagerTheme.Classic).
        /// </summary>
        public string? ClassToPagerContainer { get; set; }

        /// <summary>
        /// Css class to append to &lt;ul&gt; element in the paging content, by default it is set to pagination (from PagerSettings.DefaultTheme).
        /// </summary>
        public string? ClassToUl { get; set; }

        /// <summary>
        /// Css class to append to &lt;li&gt; element in the paging content, by default it is set to page-item (from PagerSettings.DefaultTheme).
        /// </summary>
        public string? ClassToLi { get; set; }

        /// <summary>
        /// Css class to append to &lt;a&gt;/&lt;span&gt; element in the paging content, by default it is set to page-link (from PagerSettings.DefaultTheme).
        /// </summary>
        public string? PageClass { get; set; }

        /// <summary>
        /// Css class to append to &lt;li&gt; element if active in the paging content, by default it is set to active (from PagerSettings.DefaultTheme).
        /// </summary>
        public string? ClassToActiveLi { get; set; }

        /// <summary>
        /// Displaying current page number and total number of pages in pager, by default it is set to false.
        /// </summary>
        /// <example>
        /// Page 10 of 20.
        /// </example>
        public bool HasPagerText { get; set; }

        /// <summary>
        /// Text format will display if HasPagerText is true. Use {0} to refer the current page and {1} to refer total number of pages, by default it is set to Page {0} of {1}.
        /// </summary>
        /// <example>
        /// Page 10 of 20.
        /// </example>
        public string PagerTextFormat { get; set; }

        /// <summary>
        /// Displaying start item, last item and total entries in pager, by default it is set to false.
        /// </summary>
        /// <example>
        /// Showing 1 to 10 of 30 entries.
        /// </example>
        public bool HasEntriesText { get; set; }

        /// <summary>
        /// Text format will display if HasEntriesText is true. {0} refers first entry on page, {1} refers last item on page and {2} refers total number of entries, by default it is set to Showing {0} to {1} of {2} entries.
        /// </summary>
        /// <example>
        /// Showing 1 to 10 of 30 entries.
        /// </example>
        public string EntriesTextFormat { get; set; }

        /// <summary>
        /// Accessible name for the pager landmark (aria-label on the container), by default it is set to Pagination. Set to null to omit.
        /// </summary>
        public string? AriaLabel { get; set; }

        /// <summary>
        /// Css class to append to the &lt;a&gt; element of the current page, in addition to <see cref="ClassToActiveLi"/> on the &lt;li&gt;.
        /// Useful for frameworks that style the link instead of the list item (Bulma, Tailwind). By default null.
        /// </summary>
        public string? ClassToActiveLink { get; set; }

        /// <summary>
        /// Css class to append to &lt;li&gt; elements that are disabled (unavailable first/previous/next/last links, pager text, ellipses), by default it is set to disabled (from PagerSettings.DefaultTheme).
        /// </summary>
        public string? ClassToDisabledLi { get; set; }

        /// <summary>
        /// Css class to append to disabled &lt;a&gt; elements (unavailable first/previous/next/last links). By default null.
        /// </summary>
        public string? ClassToDisabledLink { get; set; }

        /// <summary>
        /// Optional localizer. When set, every label and format (TextToFirstPage, PagerTextFormat, EntriesTextFormat, AriaLabel, ...)
        /// is used as a resource key and replaced by its localized value before formatting. By default null.
        /// </summary>
        /// <example>
        /// <code>
        /// new PagerOptions { Localizer = localizerFactory.Create(typeof(SharedResource)) }
        /// </code>
        /// </example>
        public IStringLocalizer? Localizer { get; set; }

        /// <summary>
        /// Element used for text that is not a link (pager text, entries text, ellipses): <c>a</c> (the markup of P.Pager 9.0, used by
        /// <see cref="PagerTheme.Classic"/>) or <c>span</c>. Set by the theme.
        /// </summary>
        public string TextElement { get; set; } = "a";

    }

    /// <summary>
    /// Ready-made <see cref="PagerOptions"/> presets.
    /// </summary>
    public class PrePagerOptions
    {
        ///<summary>
        /// Shows only the Previous and Next links.
        ///</summary>
        public static PagerOptions Minimal => new()
        {
            DisplayFirstPage = PagerDisplayMode.Never,
            DisplayLastPage = PagerDisplayMode.Never,
            DisplayPreviousPage = PagerDisplayMode.Always,
            DisplayNextPage = PagerDisplayMode.Always,
            HasIndividualPages = false
        };

        ///<summary>
        /// Shows Previous and Next links along with current page number and total number of pages in pager.
        ///</summary>
        public static PagerOptions MinimalWithPagerText => new()
        {
            DisplayFirstPage = PagerDisplayMode.Never,
            DisplayLastPage = PagerDisplayMode.Never,
            DisplayPreviousPage = PagerDisplayMode.Always,
            DisplayNextPage = PagerDisplayMode.Always,
            HasIndividualPages = false,
            HasPagerText = true
        };


        ///<summary>
        /// Shows Previous and Next links along with index of start and last item and total entries in pager.
        ///</summary>
        public static PagerOptions MinimalWithEntriesText => new()
        {
            DisplayFirstPage = PagerDisplayMode.Never,
            DisplayLastPage = PagerDisplayMode.Never,
            DisplayPreviousPage = PagerDisplayMode.Always,
            DisplayNextPage = PagerDisplayMode.Always,
            HasIndividualPages = false,
            HasEntriesText = true
        };

        /// <summary>
        /// Shows Previous and Next page always with default, 5 pages.
        /// </summary>
        public static PagerOptions ClassicPager => new()
        {
            DisplayFirstPage = PagerDisplayMode.Never,
            DisplayLastPage = PagerDisplayMode.Never,
            DisplayPreviousPage = PagerDisplayMode.Always,
            DisplayNextPage = PagerDisplayMode.Always
        };

        /// <summary>
        /// Shows Last, First, Previous and Next page always with default, 5 pages. 
        /// </summary>
        public static PagerOptions ClassicPagerWithFirstAndLastPages => new()
        {
            DisplayFirstPage = PagerDisplayMode.Always,
            DisplayLastPage = PagerDisplayMode.Always,
            DisplayPreviousPage = PagerDisplayMode.Always,
            DisplayNextPage = PagerDisplayMode.Always
        };

        /// <summary>
        /// Default layout with Bootstrap 4/5 classes (the markup of P.Pager 8 and earlier). To use Bootstrap everywhere, set
        /// <c>PagerSettings.DefaultTheme = PagerTheme.Bootstrap</c> at startup instead.
        /// </summary>
        public static PagerOptions Bootstrap => new PagerOptions().ApplyTheme(PagerTheme.Bootstrap);

        /// <summary>
        /// Default layout with Bulma classes (https://bulma.io/documentation/components/pagination/).
        /// </summary>
        public static PagerOptions Bulma => new PagerOptions().ApplyTheme(PagerTheme.Bulma);

        /// <summary>
        /// Default layout with Tailwind CSS utility classes. Add these classes to your Tailwind safelist, or copy them into your own theme.
        /// </summary>
        public static PagerOptions Tailwind => new PagerOptions().ApplyTheme(PagerTheme.Tailwind);

        /// <summary>Default layout with framework-neutral classes, styled by <c>_content/P.Pager.Mvc.Core/pager.css</c>.</summary>
        public static PagerOptions Neutral => new PagerOptions().ApplyTheme(PagerTheme.Neutral);

        /// <summary>Default layout with Foundation 6 classes.</summary>
        public static PagerOptions Foundation => new PagerOptions().ApplyTheme(PagerTheme.Foundation);

        /// <summary>Default layout with UIkit 3 classes.</summary>
        public static PagerOptions UIkit => new PagerOptions().ApplyTheme(PagerTheme.UIkit);

        /// <summary>Default layout with Materialize classes.</summary>
        public static PagerOptions Materialize => new PagerOptions().ApplyTheme(PagerTheme.Materialize);

    }

    /// <summary>
    /// A tri-state enum that controls the visibility of portions of the PagerList paging control.
    /// </summary>
    public enum PagerDisplayMode
    {
        /// <summary>
        /// Always render.
        /// </summary>
        Always,

        /// <summary>
        /// Never render.
        /// </summary>
        Never,

        /// <summary>
        /// Only render when there is data that makes sense to show (context sensitive).
        /// </summary>
        IfNeeded
    }
}
