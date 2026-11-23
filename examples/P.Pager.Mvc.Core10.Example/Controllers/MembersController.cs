using Microsoft.AspNetCore.Mvc;
using P.Pager.Mvc.Core10.Example.Data;
using P.Pager.Mvc.Core10.Example.Models;

namespace P.Pager.Mvc.Core10.Example.Controllers;

public class MembersController(MemberStore store) : Controller
{
    // The allow-list of sort fields: ?sort= comes from the user, so only these names can reach the query.
    private static readonly SortMap<Member> MemberSorts = new SortMap<Member>()
        .Add("id", m => m.Id)
        .Add("name", m => m.Name)
        .Add("city", m => m.City)
        .Add("joined", m => m.Joined)
        .Default("id")
        .ThenBy(m => m.Id); // unique tiebreaker so pages never repeat or skip rows

    /// <summary>
    /// Offset paging with a total count: page numbers, sorting and display options, all from the query string.
    /// For example /Members?page=3&amp;pageSize=25&amp;sort=-joined&amp;theme=Bootstrap.
    /// </summary>
    public IActionResult Index([FromQuery] PageRequest request, string theme = "Neutral", int pagesToDisplay = 5,
        bool firstLast = true, bool numbers = true)
    {
        // With EF Core, use ToPagerListAsync(request, (q, ct) => q.CountAsync(ct), (q, ct) => q.ToListAsync(ct)).
        IPager<Member> members = store.Members
            .ApplySort(request.Sort, MemberSorts)
            .ToPagerList(request, defaultPageSize: 10, maxPageSize: 50);

        return View(new MembersViewModel
        {
            Members = members,
            Sort = request.Sort ?? "id",
            Theme = theme,
            PagesToDisplay = Math.Clamp(pagesToDisplay, 1, 15),
            FirstLast = firstLast,
            Numbers = numbers,
        });
    }

    /// <summary>Every pager layout, theme and text option on one page.</summary>
    public IActionResult Gallery(int page = 1) =>
        View(store.Members.OrderBy(m => m.Id).ToPagerList(page, 5));

    /// <summary>
    /// Count-free paging: ToSlice reads one extra row instead of running COUNT, so only First / Previous / Next are shown.
    /// </summary>
    public IActionResult Slice([FromQuery] PageRequest request) =>
        View(store.Members
            .OrderByDescending(m => m.Joined).ThenBy(m => m.Id)
            .ToSlice(request, defaultPageSize: 15, maxPageSize: 50));

    /// <summary>
    /// Keyset (cursor) paging: /Members/Cursor?after=40 seeks to the rows after Id 40 instead of skipping rows,
    /// so deep pages are as fast as the first one and rows inserted meanwhile don't shift the pages.
    /// </summary>
    public IActionResult Cursor(int? after, int? before, int? pageSize)
    {
        int size = PagerExtension.ClampPageSize(pageSize, defaultPageSize: 10, maxPageSize: 50);

        if (before is int beforeKey)
        {
            // Going back: read the rows before the cursor in descending order, then flip them for display.
            // HasNextPage here means "more rows further back", that is, a Previous page exists.
            KeysetPage<Member, int> back = store.Members.ToKeysetPage(m => m.Id, beforeKey, size, descending: true);
            if (back.HasNextPage)
            {
                List<Member> items = back.Items.Reverse().ToList();
                return View(new CursorViewModel
                {
                    Members = items,
                    PageSize = size,
                    PreviousBefore = items[0].Id,
                    NextAfter = items[^1].Id,
                });
            }

            // Reached the start: show a full first page rather than the few rows left before the cursor.
            after = null;
        }

        KeysetPage<Member, int> page = after is int afterKey
            ? store.Members.ToKeysetPage(m => m.Id, afterKey, size)
            : store.Members.ToKeysetPage(m => m.Id, size);

        return View(new CursorViewModel
        {
            Members = page.Items,
            PageSize = size,
            PreviousBefore = after is not null && page.Items.Count > 0 ? page.Items[0].Id : null,
            NextAfter = page.HasNextPage ? page.NextKey : null,
        });
    }
}
