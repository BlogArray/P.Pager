# Version history

## 10.0.0

A major version: it changes defaults and removes deprecated APIs. Every intentional break is listed below (and in
`CompatibilitySuppressions.xml`, which package validation checks against the published 9.0.0).

**Target frameworks:** P.Pager: .NET Standard 2.0, .NET Standard 2.1, .NET 8, 9, 10 and 11. P.Pager.Mvc.Core and P.Pager.Blazor: .NET 8, 9, 10 and 11.

**Fixes:** `SortMap.Add` now rejects names like `" -name"` (leading space before `-` or `+`), which were accepted but could never be matched.

### Upgrading from 9.x

| Change in 10.0 | What to do |
| --- | --- |
| The default theme is `PagerTheme.Neutral` (classes `pager`, `pager-list`, `pager-item`, `pager-link`, `is-active`, `is-disabled`; text items are `<span>`). 9.x rendered Bootstrap classes inside `<div class="container">`. | To keep the 9.x markup, add `PagerSettings.DefaultTheme = PagerTheme.Classic;` at startup. For Bootstrap without the `container` class, use `PagerTheme.Bootstrap`. Otherwise include `_content/P.Pager.Mvc.Core/pager.css`. |
| P.Pager.Mvc.Core targets .NET 8, 9, 10 and 11 (no longer .NET 6 and 7). | .NET 6/7 apps stay on 9.1.0. |
| `pageSize` must be at least 1: `ToPagerList(page, 0)` throws `ArgumentOutOfRangeException` (9.x returned an empty page). | Validate or clamp untrusted sizes, for example with `PagerExtension.ClampPageSize` or `PageRequest`. |
| `PagerExtension.ToListAsync(IEnumerable<T>, CancellationToken)` is removed. | Use your provider's `ToListAsync` (for example EF Core's), or `ToPagerListAsync(page, size, countAsync, toListAsync)`. |
| `IPager.CurrentPageIndex`, `PageSize` and `TotalItemCount` are read-only (`BasePager<T>` setters are `protected`). | Create a new page instead of changing one, for example with `ToPagerList` or `Map`. |
| `Html.Pager(...)` and `Html.SlicePager(...)` return `IHtmlContent` instead of `HtmlString`. | Razor views need no change. Code that stores the result in an `HtmlString` variable should use `IHtmlContent` (or `var`). |

Apps that need none of the above can upgrade without code changes.

## 9.1.0

A backward-compatible update of 9.0.0. Apps that upgrade get the same HTML, the same data and the same API as with 9.0.0
(checked by package validation against the published 9.0.0 and by tests that compare against its output). The only
differences are the bug fixes listed below.

### Target frameworks
- **P.Pager**: .NET Standard 2.0, .NET Standard 2.1, .NET 8, .NET 9, .NET 10 (.NET 6/7 apps use the .NET Standard 2.1 build).
- **P.Pager.Mvc.Core**: .NET 6, .NET 7, .NET 8, .NET 9, .NET 10.
- **P.Pager.Blazor** (new): .NET 8, .NET 9, .NET 10.
- **P.Pager.Mvc** (.NET Framework) is no longer developed; 3.0.0 stays available on NuGet.

### Bug fixes
These change output only where 9.0.0 returned wrong results:
- A very large page number (e.g. `?page=300000000`) returned page 1's items labelled as that page (or looped for seconds); it now returns the last page.
- Empty sources: `IsLastPage` is now true and `StartItemIndex` is 0 (was "Showing 1 to 0 of 0"); the pager no longer renders a link to `page=0` or an enabled-looking Next button.
- Custom labels containing `{` / `}` (inline SVG, CSS, …) no longer throw `FormatException`.
- `PagesToDisplay` values below 1 no longer throw.
- `pageSize` 0 still returns an empty page (no exception), but now reports `TotalPageCount` 0 and no next page instead of 2,147,483,647 pages.
- Rendered attributes are no longer entity-encoded character by character (`href="/items?page=2"` instead of `href="&#x2F;&#x69;…"`; browsers read both the same).

### Additive markup (no visual change)
- `role="navigation"` and `aria-label` on the pager container (new `PagerOptions.AriaLabel`), `aria-current="page"` on the current page, `aria-disabled="true"` on disabled links.
- `rel="prev"` / `rel="next"` are only emitted on enabled links.

### New
- Themes, all opt-in: `PagerTheme.Classic` (the 9.0 markup, still the default), `Neutral` (framework-neutral, styled by the new `pager.css` in P.Pager.Mvc.Core and P.Pager.Blazor), `Bootstrap` (without the `container` class that clashes with Bootstrap's layout), `Bulma`, `Tailwind`, `Foundation`, `UIkit`, `Materialize`, or your own. Use `PagerSettings.DefaultTheme`, `new PagerOptions(theme)`, `PagerOptions.ApplyTheme(...)`, the matching `PrePagerOptions` presets, or the Blazor `Theme` parameter. New options: `ClassToActiveLink`, `ClassToDisabledLi`, `ClassToDisabledLink`, `TextElement`.
- `ToPagerListAsync(pageIndex, pageSize, countAsync, toListAsync, cancellationToken)` for truly async paging with EF Core or any async LINQ provider.
- `<pager for="Model" />` Tag Helper, and `Html.Pager(pager)` without a URL function (keeps the current query string, changes only `page`).
- `PageRequest` (binds `page`, `pageSize`, `sort` from the query string) with `ToPagerList(request)`, `ToPagerListAsync(request, ...)`, `ToSlice(request)`.
- `SortMap<T>` + `ApplySort(sort, sortMap)`: allow-listed multi-field sorting (`name,-created`) with a default sort and a unique tiebreaker.
- `PagerExtension.ClampPageSize(pageSize, defaultPageSize, maxPageSize)` for untrusted page sizes.
- `Map(...)` converts page items (for example to DTOs) and keeps the paging metadata.
- Count-free paging: `ToSlice` / `ToSliceAsync` (`ISlice<T>`), rendered with `Html.SlicePager`, `<pager slice="..." />` or `<Pagination Slice="..." />`. `ToPagerList(pageIndex, pageSize, totalItemCount)` skips the COUNT query when the total is known.
- Keyset (cursor) paging: `ToKeysetPage` / `ToKeysetPageAsync` (`KeysetPage<T, TKey>`).
- Web APIs: `PagedResult<T>` / `PagerMetadata` (`ToPagedResult()`, `ToMetadata()`) and `Response.AddPaginationHeaders(pager)` (`X-Pagination`, `X-Total-Count`, RFC 8288 `Link`).
- `PagerSettings.UnorderedQueryBehavior` (Ignore by default / Warn / Throw) to catch database queries paged without `OrderBy`.
- `PagerOptions.Localizer` (`IStringLocalizer`) to translate labels.
- P.Pager.Blazor: `<Pagination>` component (defaults to the Neutral theme unless `PagerSettings.DefaultTheme` is set).
- `ToPagerListAsync` overloads now default to `pageIndex = 1, pageSize = 10` like `ToPagerList`.
- In-memory collections are paged with LINQ to Objects (faster, enumerates lazy sequences once); `IQueryable` sources are unchanged.
- XML documentation, nullable annotations, SourceLink, symbol packages, package validation; the .NET 8+ builds are trimming / Native AOT compatible.
- Verified against EF Core (SQLite): offset paging, async overloads, keyset paging (int/string/Guid/DateTime keys), sorting, slices and unordered-query detection all translate to SQL.

### Deprecated (still work, no compiler warning; removed in v10)
Marked as deprecated in the XML documentation only, so no build breaks (including builds that treat warnings as errors).
- `PagerExtension.ToListAsync(IEnumerable<T>, CancellationToken)`: it only wraps `ToList()` in `Task.Run`.
- The setters of `IPager.CurrentPageIndex`, `PageSize` and `TotalItemCount`.

### Planned for v10 (breaking)
- Make `PagerTheme.Neutral` (or Bootstrap) the default instead of `Classic`.
- `pageSize` 0 throws instead of returning an empty page; drop .NET 6/7 from P.Pager.Mvc.Core.
- Remove the deprecated members above.
