# <p align="center"> <img src="https://raw.githubusercontent.com/BlogArray/P.Pager/master/icon.png" width="25" height="25" title="Github Logo"> P.Pager</p>

## What is this?

P.Pager is a lightweight package for paging through any `IEnumerable`/`IQueryable`: chop it up into "pages" and grab a specific page by index. The core package (.NET Standard 2.0+) works in any .NET app: ASP.NET Core, Blazor, console, WinForms, WPF, MAUI. P.Pager.Mvc.Core and P.Pager.Blazor render framework-neutral markup with a built-in stylesheet by default, and ship themes for Bootstrap, Bulma, Tailwind CSS, Foundation, UIkit and Materialize (see [Styling and themes](#styling-and-themes)).

> **Upgrading from 9.x?** See the [10.0 upgrade guide](docs/version-history.md#upgrading-from-9x). One line keeps the 9.x markup: `PagerSettings.DefaultTheme = PagerTheme.Classic;`.

## How do I use it?

### Installation

#### .Net Framework ( > 4.5.2)

Install [P.Pager.Mvc](https://www.nuget.org/packages/P.Pager.Mvc/3.0.0) via [NuGet](http://nuget.org). This will install [P.Pager](https://www.nuget.org/packages/P.Pager/3.0.0) automatically.

```
Install-Package P.Pager.Mvc -Version 3.0.0
```

#### .Net Core 3 to 5

Install [P.Pager.Mvc.Core](https://www.nuget.org/packages/P.Pager.Mvc.Core/3.0.0) via [NuGet](http://nuget.org). This will install [P.Pager](https://www.nuget.org/packages/P.Pager/3.0.0) automatically.

```
Install-Package P.Pager.Mvc.Core -Version 3.0.0
```

#### .NET 6 and 7

Install [P.Pager.Mvc.Core 9.1.0](https://www.nuget.org/packages/P.Pager.Mvc.Core/9.1.0) via [NuGet](http://nuget.org) (the last version that supports .NET 6 and 7). This will install [P.Pager](https://www.nuget.org/packages/P.Pager/) automatically.

```
Install-Package P.Pager.Mvc.Core -Version 9.1.0
```

#### .NET 8 and above

Install [P.Pager.Mvc.Core](https://www.nuget.org/packages/P.Pager.Mvc.Core/) via [NuGet](http://nuget.org). This will install [P.Pager](https://www.nuget.org/packages/P.Pager/) automatically.

```
Install-Package P.Pager.Mvc.Core
```

#### Blazor (.NET 8 and above)

Install [P.Pager.Blazor](https://www.nuget.org/packages/P.Pager.Blazor/) via [NuGet](http://nuget.org). See [Blazor](#blazor).

```
Install-Package P.Pager.Blazor
```

### After that

1. In your controller code, call **ToPagerList** off of your IEnumerable/IQueryable passing in the page size.

```csharp

public class HomeController : Controller
{
    readonly DemoData _data;
    public HomeController()
    {
        _data = new DemoData();
    }

    public ActionResult Index(int page = 1, int pageSize = 10)
    {
        var pager = _data.GetMembers().ToPagerList(page, pageSize);
        // will only contain 10 members max because of the pageSize.
        return View(pager);
    }
}
```

#### Async with Entity Framework Core

`ToPagerListAsync` has an overload that takes your provider's async operators, so both the count and the page query run truly asynchronously (P.Pager itself has no EF Core dependency):

```csharp
using Microsoft.EntityFrameworkCore;

public async Task<IActionResult> Index(int page = 1, CancellationToken cancellationToken = default)
{
    var pager = await _db.Members
        .OrderBy(m => m.Id)                      // always order before paging
        .ToPagerListAsync(page, 20,
            (q, ct) => q.CountAsync(ct),
            (q, ct) => q.ToListAsync(ct),
            cancellationToken);
    return View(pager);
}
```

Page numbers below 1 return the first page and page numbers past the end return the last page. `pageSize` must be at least 1.

2. Pass the result of **ToPagerList** to your view where you can enumerate over it - its still an IEnumerable, but only contains a child of the original data.

3. Call **Html.Pager**, passing in the instance of the Pager and a function that will generate URLs for each page to see a paging control.

```csharp
//Default Pager options
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }))
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/1.default.PNG)

#### Tag Helper

Register the Tag Helper once in `Views/_ViewImports.cshtml`:

```cshtml
@addTagHelper *, P.Pager.Mvc.Core
```

Then:

```cshtml
<pager for="(IPager)Model" />
<pager for="(IPager)Model" options="PrePagerOptions.Minimal" page-parameter="p" />
<pager for="(IPager)Model" page-url="@(p => Url.Action("Index", new { page = p }))" />
```

#### Keep the query string (filters, sorting)

Without a URL function, links are the current URL with only the `page` parameter changed, so `?search=abc&sort=name` is kept:

```cshtml
@Html.Pager((IPager)Model)
@Html.Pager((IPager)Model, PrePagerOptions.ClassicPager, pageParameterName: "p")
```

#### Limit the page size

Never pass a `pageSize` from the query string straight through, or `?pageSize=1000000` loads the whole table:

```csharp
public IActionResult Index(int page = 1, int? pageSize = null)
{
    int size = PagerExtension.ClampPageSize(pageSize, defaultPageSize: 20, maxPageSize: 100);
    return View(_db.Members.OrderBy(m => m.Id).ToPagerList(page, size));
}
```

#### Paging parameters and safe sorting from the query string

`PageRequest` binds `page`, `pageSize` and `sort` from the query string (MVC action parameter, `[FromQuery]`, or `[AsParameters]` in Minimal APIs) and applies safe defaults and a maximum page size. `SortMap` is an allow-list of sortable fields, so `?sort=` can't reach other columns:

```csharp
static readonly SortMap<Member> MemberSorts = new SortMap<Member>()
    .Add("name", m => m.Name)
    .Add("created", m => m.Created)
    .Default("-created")      // used when ?sort is missing or invalid
    .ThenBy(m => m.Id);       // unique tiebreaker: pages never repeat or skip rows

// GET /members?page=2&pageSize=50&sort=name,-created
app.MapGet("/members", ([AsParameters] PageRequest request, AppDb db) =>
    db.Members.ApplySort(request.Sort, MemberSorts)
              .ToPagerList(request, defaultPageSize: 20, maxPageSize: 100)
              .ToPagedResult());
```

A leading `-` sorts descending. Unknown fields are ignored, and at most `MaxFields` (3) fields are used.

#### Convert entities to DTOs

`Map` converts the items on the page and keeps the paging metadata:

```csharp
IPager<MemberDto> page = db.Members.OrderBy(m => m.Id)
    .ToPagerList(pageIndex, 20)
    .Map(m => new MemberDto(m.Id, m.Name));
```

`Map` runs in memory on the current page. To load fewer columns, `Select` into the DTO before paging instead.

#### Skip the COUNT query

Counting a large table can be the slowest part of paging. A slice fetches one extra row to know whether there is a next page and never runs `COUNT(*)`:

```csharp
ISlice<Member> slice = db.Members.OrderBy(m => m.Id).ToSlice(pageIndex, 20);
// async: await query.ToSliceAsync(pageIndex, 20, (q, ct) => q.ToListAsync(ct), ct);
```

```cshtml
@Html.SlicePager(Model)          @* First / Previous / Next only *@
<pager slice="Model" />
```

```razor
<Pagination Slice="slice" OnPageChanged="LoadPage" />
```

If you already know (or cache) the total, pass it and no count is run: `query.ToPagerList(pageIndex, 20, totalItemCount: cachedTotal)`.

#### Warn about unordered queries

Paging a database query without `OrderBy` returns rows in an undefined order, so pages can repeat or skip items. Turn on a check at startup:

```csharp
PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Throw; // or Warn (System.Diagnostics.Trace); default Ignore
```

In-memory sources (`List<T>`, arrays) are never flagged.

## Web APIs

Return items and metadata as JSON:

```csharp
[HttpGet]
public PagedResult<Member> Get(int page = 1) =>
    _db.Members.OrderBy(m => m.Id).ToPagerList(page, 20).ToPagedResult();
// { "items": [...], "metadata": { "currentPageIndex": 1, "pageSize": 20, "totalItemCount": 106, "totalPageCount": 6, ... } }
```

Or keep the body a plain array and put paging in headers (`X-Pagination` JSON, `X-Total-Count`, and an RFC 8288 `Link` header with first/prev/next/last):

```csharp
[HttpGet]
public IEnumerable<Member> Get(int page = 1)
{
    var pager = _db.Members.OrderBy(m => m.Id).ToPagerList(page, 20);
    Response.AddPaginationHeaders(pager);
    return pager;
}
```

If browsers on other origins read these headers, add them to your CORS policy with `WithExposedHeaders("X-Pagination", "X-Total-Count", "Link")`.

## Keyset (cursor) paging

`Skip` gets slower the deeper you page, because the database still reads every skipped row. For large tables or infinite scroll, use keyset paging: it seeks straight to the last key you saw. It has no total count or page numbers.

```csharp
// First page
KeysetPage<Member, int> page = _db.Members.ToKeysetPage(m => m.Id, pageSize: 20);

// Next page
page = _db.Members.ToKeysetPage(m => m.Id, after: page.NextKey, pageSize: 20);

// Async with EF Core
page = await _db.Members.ToKeysetPageAsync(m => m.Id, after: lastId, pageSize: 20,
    (q, ct) => q.ToListAsync(ct), cancellationToken: ct);
```

The key must be unique (usually the primary key). Numbers, `DateTime`, `string` and `Guid` keys are supported; pass `descending: true` for newest first.

## Blazor

```razor
@using P.Pager.Blazor

<Pagination Pager="pager" OnPageChanged="LoadPage" />

@code {
    IPager<Member>? pager;

    protected override Task OnInitializedAsync() => LoadPage(1);

    async Task LoadPage(int page) =>
        pager = await Db.Members.OrderBy(m => m.Id).ToPagerListAsync(page, 20,
            (q, ct) => q.CountAsync(ct), (q, ct) => q.ToListAsync(ct));
}
```

For static server rendering or shareable URLs, render links instead of buttons:

```razor
<Pagination Pager="pager" PageUrl="@(p => $"/members?page={p}")" />
```

Parameters: `PagesToDisplay` (5), `ShowFirstLast`, `ShowPreviousNext`, `ShowPageNumbers`, `HideWhenSinglePage`, `FirstText`/`PreviousText`/`NextText`/`LastText`, `AriaLabel`, and the CSS classes `Theme` (for example `PagerTheme.Bootstrap`; default `PagerSettings.DefaultTheme`), and single-class overrides `ContainerClass`, `ListClass`, `ItemClass`, `LinkClass`, `ActiveClass`, `ActiveLinkClass`, `DisabledClass`, `DisabledLinkClass`.

## Styling and themes

### Default theme

By default the pager uses framework-neutral classes (`pager`, `pager-list`, `pager-item`, `pager-link`, `is-active`, `is-disabled`), from `PagerTheme.Neutral`. Include the stylesheet that ships in the package:

```html
<!-- MVC / Razor Pages -->
<link rel="stylesheet" href="~/_content/P.Pager.Mvc.Core/pager.css" />
<!-- Blazor -->
<link rel="stylesheet" href="_content/P.Pager.Blazor/pager.css" />
```

It supports dark mode and is customized with CSS variables:

```css
.pager { --pager-active-bg: #7c3aed; --pager-radius: 999px; }
```

### Using a CSS framework

A `PagerTheme` is the set of class names; the same theme works in MVC and Blazor. Built in:

| Theme | For |
| --- | --- |
| `PagerTheme.Classic` | The 9.x markup: Bootstrap classes plus `class="container"` on the wrapper. Use it to keep 9.x output after upgrading. |
| `PagerTheme.Bootstrap` | Bootstrap 4 and 5, without the `container` class (which adds Bootstrap's layout padding). |
| `PagerTheme.Neutral` | No framework; style with `pager.css`. The default. |
| `PagerTheme.Bulma`, `PagerTheme.Tailwind`, `PagerTheme.Foundation`, `PagerTheme.UIkit`, `PagerTheme.Materialize` | Those frameworks. For Tailwind, safelist the classes. |

```csharp
// Everywhere, once at startup (Program.cs):
PagerSettings.DefaultTheme = PagerTheme.Bootstrap;
```

```cshtml
@* One pager *@
@Html.Pager((IPager)Model, PrePagerOptions.Bootstrap)
@Html.Pager((IPager)Model, new PagerOptions(PagerTheme.Bootstrap) { PagesToDisplay = 7 })

@* A layout preset with a theme *@
@Html.Pager((IPager)Model, PrePagerOptions.Minimal.ApplyTheme(PagerTheme.Bulma))
```

```razor
<Pagination Pager="pager" Theme="PagerTheme.Bootstrap" ListClass="pagination pagination-sm" />
```

Adjust a copy of a theme, or build your own:

```csharp
PagerTheme theme = PagerTheme.Bootstrap.Clone();
theme.List = "pagination justify-content-center";
```

> **Upgrading from 9.x:** add `PagerSettings.DefaultTheme = PagerTheme.Classic;` to keep the exact 9.x markup, or `PagerTheme.Bootstrap` for the same Bootstrap look without the `container` class on the wrapper.

## PagerOptions

Default options for rendering pagination.

#### Properties

| Option                | Type                                  | Summary                                                                                                                                                                                                             | Default                            |
| --------------------- | ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------- |
| DisplayFirstPage      | [PagerDisplayMode](#pagerdisplaymode) | If set to Always, render a hyperlink to the first page in the list. If set to IfNeeded, render the hyperlink only when the first page isn't visible in the paging control.                                          | IfNeeded                           |
| DisplayLastPage       | [PagerDisplayMode](#pagerdisplaymode) | If set to Always, render a hyperlink to the last page in the list. If set to IfNeeded, render the hyperlink only when the last page isn't visible in the paging control.                                            | IfNeeded                           |
| DisplayPreviousPage   | [PagerDisplayMode](#pagerdisplaymode) | If set to Always, render a hyperlink to the previous page of the list. If set to IfNeeded, render the hyperlink only when there is a previous page in the list.                                                     | IfNeeded                           |
| DisplayNextPage       | [PagerDisplayMode](#pagerdisplaymode) | If set to Always, render a hyperlink to the next page of the list. If set to IfNeeded, render the hyperlink only when there is a next page in the list.                                                             | IfNeeded                           |
| PagesToDisplay        | int?                                  | How many page numbers to display in pagination, by default it is 5.                                                                                                                                                 | 5                                  |
| HasIndividualPages    | bool                                  | Display pages numbers.                                                                                                                                                                                              | true                               |
| TextToIndividualPages | string                                | A formatted text to show to show inside the hyperlink. Use {0} to refer page number, by default it is set to {0}                                                                                                    | {0}                                |
| TextForDelimiter      | string                                | This will appear between each page number. If null or white space, no delimeter will display.                                                                                                                       | null                               |
| HasEllipses           | bool                                  | Adds an ellipe when all page numbers are not displaying, by default it is true.                                                                                                                                     | true                               |
| EllipsesFormat        | string                                | A formatted text shows when all pages are not displaying, by default it is &#8230;                                                                                                                                  | &#8230;                            |
| TextToFirstPage       | string                                | A formatted text to show for firstpage link, by default it is set to &lt;&lt;.                                                                                                                                      | &lt;&lt;                           |
| TextToPreviousPage    | string                                | A formatted text to show for previous page link, by default it is set to &lt;.                                                                                                                                      | &lt;                               |
| TextToNextPage        | string                                | A formatted text to show for next page link, by default it is set to &gt;.                                                                                                                                          | &gt;                               |
| TextToLastPage        | string                                | A formatted text to show for last page link, by default it is set to &gt;&gt;.                                                                                                                                      | &gt;&gt;                           |
| ClassToPagerContainer | string                                | Css class of the &lt;div&gt; wrapping the pager. From the theme.                                                                                                                                                    | pager                              |
| ClassToUl             | string                                | Css class of the &lt;ul&gt;. From the theme.                                                                                                                                                                        | pager-list                         |
| ClassToLi             | string                                | Css class of each &lt;li&gt;. From the theme.                                                                                                                                                                       | pager-item                         |
| PageClass             | string                                | Css class of each &lt;a&gt;/&lt;span&gt;. From the theme.                                                                                                                                                           | pager-link                         |
| ClassToActiveLi       | string                                | Css class added to the current page's &lt;li&gt;. From the theme.                                                                                                                                                   | is-active                          |
| HasPagerText          | bool                                  | Displaying current page number and total number of pages in pager, by default it is set to false.                                                                                                                   | false                              |
| PagerTextFormat       | string                                | Text format will display if HasPagerText is true. Use {0} to refer the current page and {1} to refer total number of pages, by default it is set to Page {0} of {1}.                                                | Page {0} of {1}.                   |
| HasEntriesText        | bool                                  | Displaying start item, last item and total entries in pager, by default it is set to false.                                                                                                                         | false                              |
| EntriesTextFormat     | string                                | Text format will display if HasEntriesText is true. {0} refers first entry on page, {1} refers last item on page and {2} refers total number of entries, by default it is set to Showing {0} to {1} of {2} entries. | Showing {0} to {1} of {2} entries. |
| AriaLabel             | string                                | Accessible name (aria-label) of the pager's navigation landmark. Set to null to omit.                                                                                                                              | Pagination                         |
| ClassToActiveLink     | string                                | Css class added to the current page's &lt;a&gt; (for frameworks that style the link, like Bulma). From the theme.                                                                                                   | null                               |
| ClassToDisabledLi     | string                                | Css class added to disabled &lt;li&gt; elements. From the theme.                                                                                                                                                    | is-disabled                        |
| ClassToDisabledLink   | string                                | Css class added to disabled &lt;a&gt; elements. From the theme.                                                                                                                                                     | null                               |
| Localizer             | IStringLocalizer                      | When set, every label and format is used as a resource key and replaced by its localized value.                                                                                                                     | null                               |

### PagerDisplayMode

A tri-state enum that controls the visibility of portions of the PagerList paging control.

```csharp
public enum PagerDisplayMode
```

| Fields   | Description                                                                  |
| -------- | ---------------------------------------------------------------------------- |
| Always   | Always render.                                                               |
| Never    | Never render.                                                                |
| IfNeeded | Only render when there is data that makes sense to show (context sensitive). |

### How to use PagerOptions?

1. Shows custom page numbers, let say 10 pages.

```csharp
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), new PagerOptions { PagesToDisplay = 10 })
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/7.custompagenumbers.PNG)

2. With Custom Page Text.

```csharp
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), new PagerOptions { TextToIndividualPages = "Page-{0}" })
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/8.CustomPageText.PNG)

3. Custom Wording. (<i>Can use for translation also.</i>)

```csharp
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), new PagerOptions { TextToPreviousPage = "Previous Page", TextToNextPage = "Next Page", TextToFirstPage = "First Page", TextToLastPage = "Last Page" })
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/9.CustomWording.PNG)

4. Custom options.

```csharp
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), new PagerOptions { TextToPreviousPage = "last-page", TextToNextPage = "next-page", TextToFirstPage = "first-page", TextToLastPage = "last-page", ClassToUl = "list-inline", ClassToLi = "list-inline-item", PageClass = "nopageclass", ClassToActiveLi = "niloclass", TextForDelimiter = " | " })
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/10.CustomOptions.PNG)

5. Custom Icon Options (Fontawesome)

```csharp
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), new PagerOptions { TextToPreviousPage = "<i class='fas fa-step-backward'></i>", TextToNextPage = "<i class='fas fa-step-forward'></i>", TextToFirstPage = "<i class='fas fa-fast-backward'></i>", TextToLastPage = "<i class='fas fa-fast-forward'></i>" })
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/11.Using%20Icons.PNG)

## PrePagerOptions

1. Minimal Pager

```csharp
//Shows only the Previous and Next links.
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), PrePagerOptions.Minimal)
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/2.%20minimal.PNG)

2. Minimal Pager with Pager Text (Page Count Text)

```csharp
//Shows Previous and Next links along with current page number and total number of pages in pager.
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), PrePagerOptions.MinimalWithPagerText)
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/3.minimalpagecount.PNG)

3. Minimal Pager with entries text (Item Count Text)

```csharp
//Shows Previous and Next links along with index of start and last item and total entries in pager.
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), PrePagerOptions.MinimalWithEntriesText)
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/4.minimalwithentries.PNG)

4. Classic Pager <small>(always shows Previous/Next links, but sometimes they are disabled)</small>

```csharp
//Shows Previous and Next page always with default, 5 pages.
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), PrePagerOptions.ClassicPager)
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/5.classic.PNG)

6. Classic Pager with First and Last<small>(Classic Pager with First and Last links, but sometimes they are disabled)</small>

```csharp
//Shows Last, First, Previous and Next page always with default, 5 pages.
@Html.Pager((IPager)Model, page => Url.Action("Index", new { page }), PrePagerOptions.ClassicPagerWithFirstAndLastPages)
```

![alt text](https://raw.githubusercontent.com/BlogArray/P.Pager/master/img/6.classiclastfirst.PNG)

7. With a CSS framework: see [Styling and themes](#styling-and-themes). Every preset above can be combined with a theme, for example `PrePagerOptions.ClassicPager.ApplyTheme(PagerTheme.Bootstrap)`. The screenshots above use the Bootstrap theme (`PagerTheme.Classic`, the 9.x default).

### Localization

Set `Localizer` to any `IStringLocalizer`. Each label and format (`"Page {0} of {1}."`, `"Pagination"`, `"&lt;"`, ...) is looked up as a key; missing keys fall back to the original text.

```csharp
@inject IStringLocalizer<SharedResource> L
@Html.Pager((IPager)Model, new PagerOptions { HasPagerText = true, Localizer = L })
```

## License

Licensed under the [MIT License](https://github.com/BlogArray/P.Pager/blob/master/LICENSE).
