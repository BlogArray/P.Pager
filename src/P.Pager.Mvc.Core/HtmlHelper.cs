using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

namespace P.Pager.Mvc.Core
{
    /// <summary>
    /// HTML helper extensions that render a pagination control for an <see cref="IPager"/>.
    /// </summary>
    public static class HtmlHelper
    {
        /// <summary>
        /// Displays a configurable paging control for instances of Pager.
        /// </summary>
        /// <param name="html">This method is meant to hook off HtmlHelper as an extension method.</param>
        /// <param name="pager">The Pager to use as the data source.</param>
        /// <param name="generatePageUrl">A function that takes the page number of the desired page and returns a URL-string that will load that page.</param>
        /// <returns>Outputs the pagination control HTML.</returns>
        public static IHtmlContent Pager(this IHtmlHelper html, IPager pager, Func<int, string?> generatePageUrl)
        {
            return Render(pager, generatePageUrl, new PagerOptions());
        }

        /// <summary>
        /// Displays a configurable paging control for instances of Pager.
        /// </summary>
        /// <param name="html">This method is meant to hook off HtmlHelper as an extension method.</param>
        /// <param name="pager">The Pager to use as the data source.</param>
        /// <param name="generatePageUrl">A function that takes the page number of the desired page and returns a URL-string that will load that page.</param>
        /// <param name="pagerOptions">Formatting options.</param>
        /// <returns>Outputs the pagination control HTML.</returns>
        public static IHtmlContent Pager(this IHtmlHelper html, IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            return Render(pager, generatePageUrl, pagerOptions);
        }

        /// <summary>
        /// Displays a paging control whose links are the current URL with only the page query-string parameter changed,
        /// so filters, sorting and other query-string values are kept.
        /// </summary>
        /// <param name="html">This method is meant to hook off HtmlHelper as an extension method.</param>
        /// <param name="pager">The Pager to use as the data source.</param>
        /// <param name="pagerOptions">Formatting options. Default options when null.</param>
        /// <param name="pageParameterName">Name of the page query-string parameter. By default page.</param>
        /// <returns>Outputs the pagination control HTML.</returns>
        public static IHtmlContent Pager(this IHtmlHelper html, IPager pager, PagerOptions? pagerOptions = null, string pageParameterName = "page")
        {
            return html == null
                ? throw new ArgumentNullException(nameof(html))
                : Render(pager, PagerUrl.ForCurrentRequest(html.ViewContext.HttpContext.Request, pageParameterName), pagerOptions ?? new PagerOptions());
        }

        /// <summary>
        /// Displays First / Previous / Next links for a count-free <see cref="ISlice"/>. Page numbers, Last, pager text and
        /// entries text need the total, so they are not shown.
        /// </summary>
        /// <param name="html">This method is meant to hook off HtmlHelper as an extension method.</param>
        /// <param name="slice">The slice to use as the data source.</param>
        /// <param name="generatePageUrl">Builds the URL for a page number. When null, the current URL with only the page parameter changed.</param>
        /// <param name="pagerOptions">Formatting options. Default options when null.</param>
        /// <param name="pageParameterName">Name of the page query-string parameter used when <paramref name="generatePageUrl"/> is null. By default page.</param>
        /// <returns>Outputs the pagination control HTML.</returns>
        public static IHtmlContent SlicePager(this IHtmlHelper html, ISlice slice, Func<int, string?>? generatePageUrl = null, PagerOptions? pagerOptions = null, string pageParameterName = "page")
        {
            if (generatePageUrl == null)
            {
                if (html == null) throw new ArgumentNullException(nameof(html));
                generatePageUrl = PagerUrl.ForCurrentRequest(html.ViewContext.HttpContext.Request, pageParameterName);
            }
            return RenderSlice(slice, generatePageUrl, pagerOptions ?? new PagerOptions());
        }

        internal static HtmlString RenderSlice(ISlice slice, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            return slice == null
                ? throw new ArgumentNullException(nameof(slice))
                : pagerOptions == null
                ? throw new ArgumentNullException(nameof(pagerOptions))
                : Render(new SliceAsPager(slice), generatePageUrl, pagerOptions.ForSlice());
        }

        /// <summary>
        /// Presents a slice as a pager whose last known page is the next one (or the current one when there is no next page).
        /// Only the members used for First/Previous/Next are meaningful.
        /// </summary>
        private sealed class SliceAsPager(ISlice slice) : IPager
        {
            public int CurrentPageIndex => slice.CurrentPageIndex;
            public int PageSize => slice.PageSize;
            public int TotalItemCount => slice.EndItemIndex;
            public int TotalPageCount => slice.HasNextPage ? slice.CurrentPageIndex + 1 : slice.CurrentPageIndex;
            public bool HasPreviousPage => slice.HasPreviousPage;
            public bool HasNextPage => slice.HasNextPage;
            public bool IsFirstPage => slice.CurrentPageIndex == 1;
            public bool IsLastPage => !slice.HasNextPage;
            public int StartItemIndex => slice.StartItemIndex;
            public int EndItemIndex => slice.EndItemIndex;
        }

        /// <summary>
        /// Renders the paging control. Shared by the HTML helper and <see cref="PagerTagHelper"/>.
        /// </summary>
        internal static HtmlString Render(IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            if (pager == null) throw new ArgumentNullException(nameof(pager));
            if (generatePageUrl == null) throw new ArgumentNullException(nameof(generatePageUrl));
            if (pagerOptions == null) throw new ArgumentNullException(nameof(pagerOptions));

            int firstPageToDisplay = 1;
            int lastPageToDisplay = pager.TotalPageCount;
            int pageNumbersToDisplay = lastPageToDisplay;

            if (pagerOptions.PagesToDisplay.HasValue && pager.TotalPageCount > pagerOptions.PagesToDisplay)
            {
                int maxPageNumbersToDisplay = Math.Max(1, pagerOptions.PagesToDisplay.Value);
                firstPageToDisplay = pager.CurrentPageIndex - (maxPageNumbersToDisplay / 2);
                if (firstPageToDisplay < 1)
                    firstPageToDisplay = 1;
                pageNumbersToDisplay = maxPageNumbersToDisplay;
                lastPageToDisplay = firstPageToDisplay + pageNumbersToDisplay - 1;
                if (lastPageToDisplay > pager.TotalPageCount)
                {
                    firstPageToDisplay = pager.TotalPageCount - maxPageNumbersToDisplay + 1;
                    lastPageToDisplay = pager.TotalPageCount;
                }
            }

            List<TagBuilder> listItemLinks = [];

            if (pagerOptions.DisplayFirstPage == PagerDisplayMode.Always || (pagerOptions.DisplayFirstPage == PagerDisplayMode.IfNeeded && firstPageToDisplay > 1))
                listItemLinks.Add(First(pager, generatePageUrl, pagerOptions));

            if (pagerOptions.DisplayPreviousPage == PagerDisplayMode.Always || (pagerOptions.DisplayPreviousPage == PagerDisplayMode.IfNeeded && !pager.IsFirstPage))
                listItemLinks.Add(Previous(pager, generatePageUrl, pagerOptions));

            if (pagerOptions.HasPagerText)
                listItemLinks.Add(PageCountAndCurrentPage(pager, pagerOptions));

            if (pagerOptions.HasEntriesText)
                listItemLinks.Add(DisplayEntriesText(pager, pagerOptions));

            if (pagerOptions.HasIndividualPages)
            {
                if (pagerOptions.HasEllipses && firstPageToDisplay > 1)
                    listItemLinks.Add(Ellipses(pagerOptions));

                foreach (int i in Enumerable.Range(firstPageToDisplay, pageNumbersToDisplay))
                {
                    //show delimiter between page numbers
                    if (i > firstPageToDisplay && !string.IsNullOrWhiteSpace(pagerOptions.TextForDelimiter))
                        listItemLinks.Add(AddToListItem(Localize(pagerOptions, pagerOptions.TextForDelimiter), pagerOptions.ClassToLi));

                    //show page number link
                    listItemLinks.Add(Page(i, pager, generatePageUrl, pagerOptions));
                }

                if (pagerOptions.HasEllipses && (firstPageToDisplay + pageNumbersToDisplay - 1) < pager.TotalPageCount)
                    listItemLinks.Add(Ellipses(pagerOptions));
            }

            if (pagerOptions.DisplayNextPage == PagerDisplayMode.Always || (pagerOptions.DisplayNextPage == PagerDisplayMode.IfNeeded && !pager.IsLastPage))
                listItemLinks.Add(Next(pager, generatePageUrl, pagerOptions));

            if (pagerOptions.DisplayLastPage == PagerDisplayMode.Always || (pagerOptions.DisplayLastPage == PagerDisplayMode.IfNeeded && lastPageToDisplay < pager.TotalPageCount))
                listItemLinks.Add(Last(pager, generatePageUrl, pagerOptions));

            string listItemLinksString = listItemLinks.Aggregate(new StringBuilder(), (sb, listItem) => sb.Append(TagBuilderToString(listItem)), sb => sb.ToString());

            TagBuilder ul = new("ul");
            AppendHtml(ul, listItemLinksString);
            AddCssClass(ul, pagerOptions.ClassToUl);
            TagBuilder outerDiv = new("div");
            AddCssClass(outerDiv, pagerOptions.ClassToPagerContainer);
            outerDiv.Attributes["role"] = "navigation";
            string? ariaLabel = Localize(pagerOptions, pagerOptions.AriaLabel);
            if (!string.IsNullOrWhiteSpace(ariaLabel))
                outerDiv.Attributes["aria-label"] = ariaLabel;
            AppendHtml(outerDiv, TagBuilderToString(ul));
            return new HtmlString(TagBuilderToString(outerDiv));
        }

        private static TagBuilder First(IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            const int targetPageNumber = 1;
            TagBuilder first = new("a");
            AppendHtml(first, LocalizeAndFormat(pagerOptions, pagerOptions.TextToFirstPage, targetPageNumber));
            AddCssClass(first, pagerOptions.PageClass);

            if (pager.IsFirstPage)
                return DisabledLink(first, pagerOptions);

            first.Attributes["href"] = generatePageUrl(targetPageNumber);
            return AddToListItem(first, pagerOptions, pagerOptions.ClassToLi);
        }

        private static TagBuilder Previous(IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            int targetPageNumber = pager.CurrentPageIndex - 1;
            TagBuilder previous = new("a");
            AppendHtml(previous, LocalizeAndFormat(pagerOptions, pagerOptions.TextToPreviousPage, targetPageNumber));
            AddCssClass(previous, pagerOptions.PageClass);

            if (!pager.HasPreviousPage)
                return DisabledLink(previous, pagerOptions);

            previous.Attributes["rel"] = "prev";
            previous.Attributes["href"] = generatePageUrl(targetPageNumber);
            return AddToListItem(previous, pagerOptions, pagerOptions.ClassToLi);
        }

        private static TagBuilder Page(int i, IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            TagBuilder page = new("a");
            SetInnerText(page, LocalizeAndFormat(pagerOptions, pagerOptions.TextToIndividualPages, i));
            AddCssClass(page, pagerOptions.PageClass);
            if (i == pager.CurrentPageIndex)
            {
                page.Attributes["aria-current"] = "page";
                AddCssClass(page, pagerOptions.ClassToActiveLink);
                return AddToListItem(page, pagerOptions, JoinClasses(pagerOptions.ClassToLi, pagerOptions.ClassToActiveLi));
            }

            page.Attributes["href"] = generatePageUrl(i);
            return AddToListItem(page, pagerOptions, null);
        }

        private static TagBuilder Next(IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            int targetPageNumber = pager.CurrentPageIndex + 1;
            TagBuilder next = new("a");
            AppendHtml(next, LocalizeAndFormat(pagerOptions, pagerOptions.TextToNextPage, targetPageNumber));
            AddCssClass(next, pagerOptions.PageClass);

            if (!pager.HasNextPage)
                return DisabledLink(next, pagerOptions);

            next.Attributes["rel"] = "next";
            next.Attributes["href"] = generatePageUrl(targetPageNumber);
            return AddToListItem(next, pagerOptions, pagerOptions.ClassToLi);
        }

        private static TagBuilder Last(IPager pager, Func<int, string?> generatePageUrl, PagerOptions pagerOptions)
        {
            int targetPageNumber = pager.TotalPageCount;
            TagBuilder last = new("a");
            AppendHtml(last, LocalizeAndFormat(pagerOptions, pagerOptions.TextToLastPage, targetPageNumber));
            AddCssClass(last, pagerOptions.PageClass);

            if (pager.IsLastPage)
                return DisabledLink(last, pagerOptions);

            last.Attributes["href"] = generatePageUrl(targetPageNumber);
            return AddToListItem(last, pagerOptions, pagerOptions.ClassToLi);
        }

        private static TagBuilder PageCountAndCurrentPage(IPager pager, PagerOptions pagerOptions)
        {
            TagBuilder text = new(TextElement(pagerOptions)); // plain text: <a> without href (9.0 markup) or <span>
            AddCssClass(text, pagerOptions.PageClass);
            SetInnerText(text, LocalizeAndFormat(pagerOptions, pagerOptions.PagerTextFormat, pager.CurrentPageIndex, pager.TotalPageCount));
            return AddToListItem(text, pagerOptions, JoinClasses(pagerOptions.ClassToLi, pagerOptions.ClassToDisabledLi));
        }

        private static TagBuilder DisplayEntriesText(IPager pager, PagerOptions pagerOptions)
        {
            TagBuilder text = new(TextElement(pagerOptions)); // plain text: <a> without href (9.0 markup) or <span>
            AddCssClass(text, pagerOptions.PageClass);
            SetInnerText(text, LocalizeAndFormat(pagerOptions, pagerOptions.EntriesTextFormat, pager.StartItemIndex, pager.EndItemIndex, pager.TotalItemCount));
            return AddToListItem(text, pagerOptions, JoinClasses(pagerOptions.ClassToLi, pagerOptions.ClassToDisabledLi));
        }

        private static TagBuilder Ellipses(PagerOptions pagerOptions)
        {
            TagBuilder a = new(TextElement(pagerOptions)); // plain text: <a> without href (9.0 markup) or <span>
            AddCssClass(a, pagerOptions.PageClass);
            AppendHtml(a, pagerOptions.EllipsesFormat);
            return AddToListItem(a, pagerOptions, JoinClasses(pagerOptions.ClassToLi, pagerOptions.ClassToDisabledLi));
        }

        private static string TextElement(PagerOptions pagerOptions) =>
            string.Equals(pagerOptions.TextElement, "span", StringComparison.OrdinalIgnoreCase) ? "span" : "a";

        private static TagBuilder DisabledLink(TagBuilder link, PagerOptions pagerOptions)
        {
            link.Attributes["aria-disabled"] = "true";
            AddCssClass(link, pagerOptions.ClassToDisabledLink);
            return AddToListItem(link, pagerOptions, JoinClasses(pagerOptions.ClassToLi, pagerOptions.ClassToDisabledLi));
        }

        private static string? Localize(PagerOptions pagerOptions, string? text)
        {
            return pagerOptions.Localizer == null || string.IsNullOrEmpty(text) ? text : pagerOptions.Localizer[text!].Value;
        }

        private static string? LocalizeAndFormat(PagerOptions pagerOptions, string? format, params object[] args)
        {
            return SafeFormat(Localize(pagerOptions, format), args);
        }

        /// <summary>
        /// Formats a user-supplied label. Labels that contain braces but aren't valid format strings
        /// (inline SVG, CSS, Alpine/Vue attributes, ...) are returned as-is instead of throwing.
        /// </summary>
        private static string? SafeFormat(string? format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                return format;
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        private static string? JoinClasses(string? a, string? b)
        {
            return string.IsNullOrWhiteSpace(a) ? b : string.IsNullOrWhiteSpace(b) ? a : a + " " + b;
        }

        private static void AddCssClass(TagBuilder tagBuilder, string? cssClass)
        {
            if (!string.IsNullOrWhiteSpace(cssClass))
                tagBuilder.AddCssClass(cssClass);
        }

        private static void AppendHtml(TagBuilder tagBuilder, string? innerHtml)
        {
            if (innerHtml != null)
                tagBuilder.InnerHtml.AppendHtml(innerHtml);
        }

        private static void SetInnerText(TagBuilder tagBuilder, string? innerText)
        {
            if (innerText != null)
                tagBuilder.InnerHtml.SetContent(innerText);
        }

        private static TagBuilder AddToListItem(string? text, string? cssClass)
        {
            TagBuilder li = new("li");
            AddCssClass(li, cssClass);
            SetInnerText(li, text);
            return li;
        }

        private static TagBuilder AddToListItem(TagBuilder inner, PagerOptions pagerOptions, string? cssClass)
        {
            TagBuilder li = new("li");
            AddCssClass(li, cssClass ?? pagerOptions.ClassToLi);
            AppendHtml(li, TagBuilderToString(inner));
            return li;
        }

        private static string TagBuilderToString(TagBuilder tagBuilder)
        {
            using StringWriter writer = new();
            tagBuilder.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }
    }
}
