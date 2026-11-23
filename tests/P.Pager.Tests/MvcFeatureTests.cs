using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using P.Pager.Mvc.Core;
using System.Text.Json;

namespace P.Pager.Tests
{
    public class MvcFeatureTests
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        private static HttpRequest Request(string pathAndQuery)
        {
            DefaultHttpContext context = new();
            int q = pathAndQuery.IndexOf('?');
            context.Request.PathBase = "/app";
            context.Request.Path = q < 0 ? pathAndQuery : pathAndQuery[..q];
            context.Request.QueryString = q < 0 ? QueryString.Empty : new QueryString(pathAndQuery[q..]);
            return context.Request;
        }

        [Fact]
        public void PagerUrl_KeepsOtherQueryValues_AndReplacesPage()
        {
            Func<int, string> url = PagerUrl.ForCurrentRequest(Request("/members?sort=name&page=3&tag=a&tag=b%20c"));
            Assert.Equal("/app/members?sort=name&tag=a&tag=b%20c&page=4", url(4));
        }

        [Fact]
        public void PagerUrl_CustomParameter_CaseInsensitive()
        {
            Func<int, string> url = PagerUrl.ForCurrentRequest(Request("/members?P=2"), "p");
            Assert.Equal("/app/members?p=5", url(5));
        }

        [Fact]
        public void HtmlHelper_WithoutUrlFunction_UsesCurrentRequest()
        {
            FakeHtmlHelper html = new(Request("/members?sort=name"));
            string output = html.Pager(Items.ToPagerList(1, 20)).ToString()!;
            Assert.Contains("href=\"/app/members?sort=name&amp;page=2\"", output);
        }

        [Fact]
        public void TagHelper_RendersPager_WithoutWrapperTag()
        {
            PagerTagHelper helper = new()
            {
                For = Items.ToPagerList(2, 20),
                ViewContext = new FakeHtmlHelper(Request("/members")).ViewContext,
            };
            TagHelperContext context = new([], new Dictionary<object, object>(), "id");
            TagHelperOutput output = new("pager", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

            helper.Process(context, output);

            Assert.Null(output.TagName);
            string html = output.Content.GetContent();
            Assert.Contains("role=\"navigation\"", html);
            Assert.Contains("href=\"/app/members?page=3\" rel=\"next\"", html);
        }

        [Fact]
        public void TagHelper_NullPager_RendersNothing()
        {
            PagerTagHelper helper = new() { For = null };
            TagHelperOutput output = new("pager", [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
            helper.Process(new TagHelperContext([], new Dictionary<object, object>(), "id"), output);
            Assert.True(output.IsContentModified == false || output.Content.GetContent() == string.Empty);
            Assert.Null(output.TagName);
        }

        [Fact]
        public void PaginationHeaders_WritesMetadataTotalAndLinks()
        {
            HttpRequest request = Request("/api/members?q=x&page=2");
            HttpResponse response = request.HttpContext.Response;

            response.AddPaginationHeaders(Items.ToPagerList(2, 20));

            Assert.Equal("200", response.Headers[PaginationHeaders.TotalCountHeaderName].ToString());
            using JsonDocument json = JsonDocument.Parse(response.Headers[PaginationHeaders.PaginationHeaderName].ToString());
            Assert.Equal(2, json.RootElement.GetProperty("currentPageIndex").GetInt32());
            Assert.Equal(10, json.RootElement.GetProperty("totalPageCount").GetInt32());
            Assert.Equal(
                "</app/api/members?q=x&page=1>; rel=\"first\", </app/api/members?q=x&page=1>; rel=\"prev\", </app/api/members?q=x&page=3>; rel=\"next\", </app/api/members?q=x&page=10>; rel=\"last\"",
                response.Headers["Link"].ToString());
        }

        [Fact]
        public void PaginationHeaders_EmptyPager_HasNoLinkHeader()
        {
            HttpResponse response = Request("/api/members").HttpContext.Response;
            response.AddPaginationHeaders(new List<int>().ToPagerList(1, 20));
            Assert.Equal("0", response.Headers[PaginationHeaders.TotalCountHeaderName].ToString());
            Assert.False(response.Headers.ContainsKey("Link"));
        }

        [Fact]
        public void BulmaPreset_PutsCurrentAndDisabledClassesOnLinks()
        {
            PagerOptions options = PrePagerOptions.Bulma;
            options.DisplayPreviousPage = PagerDisplayMode.Always;
            string html = P.Pager.Mvc.Core.HtmlHelper.Pager(null!, Items.ToPagerList(1, 20), p => "/x?page=" + p, options).ToString()!;
            Assert.StartsWith("<div aria-label=\"Pagination\" class=\"pagination\" role=\"navigation\"><ul class=\"pagination-list\">", html);
            Assert.Contains("<li><a aria-current=\"page\" class=\"pagination-link is-current\">1</a></li>", html);
            Assert.Contains("<li><a aria-disabled=\"true\" class=\"pagination-link is-disabled\">&lt;</a></li>", html);
            Assert.DoesNotContain("class=\"\"", html);
        }

        [Fact]
        public void TailwindPreset_Renders()
        {
            string html = P.Pager.Mvc.Core.HtmlHelper.Pager(null!, Items.ToPagerList(3, 20), p => "/x?page=" + p, PrePagerOptions.Tailwind).ToString()!;
            Assert.Contains("!bg-blue-600", html);
            Assert.Contains("pointer-events-none", html);
        }

        [Fact]
        public void PagesToDisplayZero_DoesNotThrow()
        {
            string html = P.Pager.Mvc.Core.HtmlHelper.Pager(null!, Items.ToPagerList(3, 20), p => "/x?page=" + p, new PagerOptions { PagesToDisplay = 0 }).ToString()!;
            Assert.Contains("aria-current=\"page\"", html);
        }

        [Fact]
        public void Localizer_TranslatesLabels()
        {
            PagerOptions options = new()
            {
                HasPagerText = true,
                Localizer = new DictionaryLocalizer(new()
                {
                    ["Page {0} of {1}."] = "Seite {0} von {1}.",
                    ["Pagination"] = "Seitennavigation",
                }),
            };
            string html = P.Pager.Mvc.Core.HtmlHelper.Pager(null!, Items.ToPagerList(3, 20), p => "/x?page=" + p, options).ToString()!;
            Assert.Contains("Seite 3 von 10.", html);
            Assert.Contains("aria-label=\"Seitennavigation\"", html);
            Assert.Contains(">&gt;</a>", html); // untranslated keys fall back to the key
        }

        private sealed class DictionaryLocalizer(Dictionary<string, string> values) : IStringLocalizer
        {
            public LocalizedString this[string name] =>
                values.TryGetValue(name, out string? v) ? new LocalizedString(name, v) : new LocalizedString(name, name, resourceNotFound: true);
            public LocalizedString this[string name, params object[] arguments] => this[name];
            public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => values.Select(kv => new LocalizedString(kv.Key, kv.Value));
        }

        private sealed class FakeHtmlHelper : IHtmlHelper
        {
            public FakeHtmlHelper(HttpRequest request)
            {
                ViewContext = new ViewContext
                {
                    HttpContext = request.HttpContext,
                    RouteData = new RouteData(),
                    ActionDescriptor = new ActionDescriptor(),
                };
            }

            public ViewContext ViewContext { get; }
            public Html5DateRenderingMode Html5DateRenderingMode { get; set; }
            public string IdAttributeDotReplacement => "_";
            public IModelMetadataProvider MetadataProvider => throw new NotSupportedException();
            public dynamic ViewBag => throw new NotSupportedException();
            public ViewDataDictionary ViewData => throw new NotSupportedException();
            public ITempDataDictionary TempData => throw new NotSupportedException();
            public System.Text.Encodings.Web.UrlEncoder UrlEncoder => System.Text.Encodings.Web.UrlEncoder.Default;
            public Microsoft.AspNetCore.Html.IHtmlContent ActionLink(string linkText, string? actionName, string? controllerName, string? protocol, string? hostname, string? fragment, object? routeValues, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent AntiForgeryToken() => throw new NotSupportedException();
            public MvcForm BeginForm(string? actionName, string? controllerName, object? routeValues, FormMethod method, bool? antiforgery, object? htmlAttributes) => throw new NotSupportedException();
            public MvcForm BeginRouteForm(string? routeName, object? routeValues, FormMethod method, bool? antiforgery, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent CheckBox(string expression, bool? isChecked, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Display(string expression, string? templateName, string? htmlFieldName, object? additionalViewData) => throw new NotSupportedException();
            public string DisplayName(string expression) => throw new NotSupportedException();
            public string DisplayText(string expression) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent DropDownList(string expression, IEnumerable<SelectListItem>? selectList, string? optionLabel, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Editor(string expression, string? templateName, string? htmlFieldName, object? additionalViewData) => throw new NotSupportedException();
            public string Encode(object? value) => throw new NotSupportedException();
            public string Encode(string? value) => throw new NotSupportedException();
            public void EndForm() => throw new NotSupportedException();
            public string FormatValue(object? value, string? format) => throw new NotSupportedException();
            public string GenerateIdFromName(string fullName) => throw new NotSupportedException();
            public IEnumerable<SelectListItem> GetEnumSelectList<TEnum>() where TEnum : struct => throw new NotSupportedException();
            public IEnumerable<SelectListItem> GetEnumSelectList(Type enumType) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Hidden(string expression, object? value, object? htmlAttributes) => throw new NotSupportedException();
            public string Id(string expression) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Label(string expression, string? labelText, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent ListBox(string expression, IEnumerable<SelectListItem>? selectList, object? htmlAttributes) => throw new NotSupportedException();
            public string Name(string expression) => throw new NotSupportedException();
            public Task<Microsoft.AspNetCore.Html.IHtmlContent> PartialAsync(string partialViewName, object? model, ViewDataDictionary? viewData) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Password(string expression, object? value, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent RadioButton(string expression, object? value, bool? isChecked, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Raw(object? value) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent Raw(string? value) => throw new NotSupportedException();
            public Task RenderPartialAsync(string partialViewName, object? model, ViewDataDictionary? viewData) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent RouteLink(string linkText, string? routeName, string? protocol, string? hostName, string? fragment, object? routeValues, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent TextArea(string expression, string? value, int rows, int columns, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent TextBox(string expression, object? value, string? format, object? htmlAttributes) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent ValidationMessage(string expression, string? message, object? htmlAttributes, string? tag) => throw new NotSupportedException();
            public Microsoft.AspNetCore.Html.IHtmlContent ValidationSummary(bool excludePropertyErrors, string? message, object? htmlAttributes, string? tag) => throw new NotSupportedException();
            public string? Value(string expression, string? format) => throw new NotSupportedException();
        }
    }
}
