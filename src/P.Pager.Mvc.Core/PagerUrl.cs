using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace P.Pager.Mvc.Core
{
    /// <summary>
    /// Builds page URLs from the current request.
    /// </summary>
    public static class PagerUrl
    {
        /// <summary>
        /// Returns a function that produces the current request's path and query string with only
        /// <paramref name="pageParameterName"/> replaced, so filters, sorting and other values are kept.
        /// </summary>
        /// <param name="request">The current request.</param>
        /// <param name="pageParameterName">Name of the page query-string parameter. By default page.</param>
        /// <returns>A page-number-to-URL function.</returns>
        public static Func<int, string> ForCurrentRequest(HttpRequest request, string pageParameterName = "page")
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(pageParameterName)) throw new ArgumentException("A page parameter name is required.", nameof(pageParameterName));

            string path = (request.PathBase + request.Path).ToString();
            List<KeyValuePair<string, string>> otherValues = request.Query
                .Where(kv => !string.Equals(kv.Key, pageParameterName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string>(kv.Key, v ?? string.Empty)))
                .ToList();

            return page =>
            {
                QueryBuilder query = new(otherValues)
                {
                    { pageParameterName, page.ToString(CultureInfo.InvariantCulture) }
                };
                return path + query.ToQueryString();
            };
        }
    }
}
