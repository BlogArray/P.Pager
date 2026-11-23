using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;

namespace P.Pager.Mvc.Core
{
    /// <summary>
    /// Renders a pagination control: <c>&lt;pager for="Model" /&gt;</c>.
    /// Register it with <c>@addTagHelper *, P.Pager.Mvc.Core</c> in _ViewImports.cshtml.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;pager for="Model" /&gt;
    /// &lt;pager for="Model" options="PrePagerOptions.Minimal" page-parameter="p" /&gt;
    /// &lt;pager for="Model" page-url="@(p =&gt; Url.Action("Index", new { page = p }))" /&gt;
    /// &lt;pager slice="Model" /&gt;   (count-free ISlice: First / Previous / Next only)
    /// </code>
    /// </example>
    [HtmlTargetElement("pager", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("pager", Attributes = SliceAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class PagerTagHelper : TagHelper
    {
        private const string ForAttributeName = "for";
        private const string SliceAttributeName = "slice";

        /// <summary>The pager to render.</summary>
        [HtmlAttributeName(ForAttributeName)]
        public IPager? For { get; set; }

        /// <summary>A count-free slice to render (First / Previous / Next only). Use instead of <see cref="For"/>.</summary>
        [HtmlAttributeName(SliceAttributeName)]
        public ISlice? Slice { get; set; }

        /// <summary>Formatting options. Default options when not set.</summary>
        [HtmlAttributeName("options")]
        public PagerOptions? Options { get; set; }

        /// <summary>
        /// Builds the URL for a page number. When not set, links are the current URL with only the page parameter changed.
        /// </summary>
        [HtmlAttributeName("page-url")]
        public Func<int, string?>? PageUrl { get; set; }

        /// <summary>Name of the page query-string parameter used when <see cref="PageUrl"/> is not set. By default page.</summary>
        [HtmlAttributeName("page-parameter")]
        public string PageParameter { get; set; } = "page";

        /// <summary>The current view context (set by MVC).</summary>
        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; } = default!;

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null; // render only the pager markup, not a <pager> element

            if (For == null && Slice == null)
            {
                output.SuppressOutput();
                return;
            }

            Func<int, string?> pageUrl = PageUrl ?? PagerUrl.ForCurrentRequest(ViewContext.HttpContext.Request, PageParameter);
            PagerOptions options = Options ?? new PagerOptions();
            output.Content.SetHtmlContent(For != null
                ? HtmlHelper.Render(For, pageUrl, options)
                : HtmlHelper.RenderSlice(Slice!, pageUrl, options));
        }
    }
}
