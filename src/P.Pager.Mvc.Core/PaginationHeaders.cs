using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace P.Pager.Mvc.Core
{
    /// <summary>
    /// Writes paging metadata to HTTP response headers for Web APIs.
    /// </summary>
    public static class PaginationHeaders
    {
        /// <summary>Name of the header that carries the JSON paging metadata.</summary>
        public const string PaginationHeaderName = "X-Pagination";

        /// <summary>Name of the header that carries the total number of items.</summary>
        public const string TotalCountHeaderName = "X-Total-Count";

        /// <summary>
        /// Adds <c>X-Pagination</c> (JSON <see cref="PagerMetadata"/>), <c>X-Total-Count</c> and an RFC 8288 <c>Link</c> header
        /// (first, prev, next, last) to the response. Links are the current URL with only the page parameter changed.
        /// Remember to expose these headers in your CORS policy if browsers on other origins read them.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="pager">The pager.</param>
        /// <param name="pageParameterName">Name of the page query-string parameter. By default page.</param>
        public static void AddPaginationHeaders(this HttpResponse response, IPager pager, string pageParameterName = "page")
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            AddPaginationHeaders(response, pager, PagerUrl.ForCurrentRequest(response.HttpContext.Request, pageParameterName));
        }

        /// <summary>
        /// Adds <c>X-Pagination</c> (JSON <see cref="PagerMetadata"/>), <c>X-Total-Count</c> and an RFC 8288 <c>Link</c> header
        /// (first, prev, next, last) to the response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="pager">The pager.</param>
        /// <param name="generatePageUrl">Builds the URL for a page number. Return null to leave a link out.</param>
        public static void AddPaginationHeaders(this HttpResponse response, IPager pager, Func<int, string?> generatePageUrl)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (pager == null) throw new ArgumentNullException(nameof(pager));
            if (generatePageUrl == null) throw new ArgumentNullException(nameof(generatePageUrl));

            response.Headers[PaginationHeaderName] = JsonSerializer.Serialize(pager.ToMetadata(), PagerJsonContext.Default.PagerMetadata);
            response.Headers[TotalCountHeaderName] = pager.TotalItemCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

            List<string> links = [];
            void Add(int page, string rel)
            {
                string? url = generatePageUrl(page);
                if (!string.IsNullOrEmpty(url))
                    links.Add($"<{url}>; rel=\"{rel}\"");
            }

            if (pager.TotalPageCount > 0)
                Add(1, "first");
            if (pager.HasPreviousPage)
                Add(pager.CurrentPageIndex - 1, "prev");
            if (pager.HasNextPage)
                Add(pager.CurrentPageIndex + 1, "next");
            if (pager.TotalPageCount > 0)
                Add(pager.TotalPageCount, "last");

            if (links.Count > 0)
                response.Headers["Link"] = string.Join(", ", links);
        }
    }

    /// <summary>
    /// Source-generated JSON metadata (camelCase, like ASP.NET Core's web defaults) so the headers work with trimming and Native AOT.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(PagerMetadata))]
    internal sealed partial class PagerJsonContext : JsonSerializerContext
    {
    }
}
