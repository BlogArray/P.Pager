using P.Pager.Mvc.Core10.Example.Data;

namespace P.Pager.Mvc.Core10.Example.Models;

/// <summary>The /Members page: one page of members plus the options chosen in the form.</summary>
public sealed class MembersViewModel
{
    public required IPager<Member> Members { get; init; }
    public required string Sort { get; init; }
    public required string Theme { get; init; }
    public required int PagesToDisplay { get; init; }
    public required bool FirstLast { get; init; }
    public required bool Numbers { get; init; }
}

/// <summary>The /Members/Cursor page: one keyset page and the cursors for the links around it.</summary>
public sealed class CursorViewModel
{
    public required IReadOnlyList<Member> Members { get; init; }
    public required int PageSize { get; init; }

    /// <summary>Cursor for the Previous link (<c>?before=</c>), or null on the first page.</summary>
    public int? PreviousBefore { get; init; }

    /// <summary>Cursor for the Next link (<c>?after=</c>), or null on the last page.</summary>
    public int? NextAfter { get; init; }
}
