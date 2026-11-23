using Bunit;
using P.Pager.Blazor;
using P.Pager.Mvc.Core;

namespace P.Pager.Tests
{
    public class MapTests
    {
        [Fact]
        public void Map_ConvertsItems_KeepsMetadata()
        {
            IPager<int> page = Enumerable.Range(1, 45).ToPagerList(3, 20);
            IPager<string> mapped = page.Map(i => $"#{i}");

            Assert.Equal(Enumerable.Range(41, 5).Select(i => $"#{i}"), mapped);
            Assert.Equal(page.CurrentPageIndex, mapped.CurrentPageIndex);
            Assert.Equal(page.PageSize, mapped.PageSize);
            Assert.Equal(page.TotalItemCount, mapped.TotalItemCount);
            Assert.Equal(page.TotalPageCount, mapped.TotalPageCount);
        }

        [Fact]
        public void Map_EmptyPage()
        {
            IPager<string> mapped = new List<int>().ToPagerList(1, 10).Map(i => i.ToString());
            Assert.Empty(mapped);
            Assert.True(mapped.IsLastPage);
        }

        [Fact]
        public void Map_Slice()
        {
            ISlice<string> mapped = Enumerable.Range(1, 45).ToSlice(2, 20).Map(i => $"#{i}");
            Assert.Equal(20, mapped.Count);
            Assert.True(mapped.HasNextPage);
            Assert.Equal(2, mapped.CurrentPageIndex);
        }
    }

    public class SliceTests
    {
        private static readonly List<int> Items = Enumerable.Range(1, 45).ToList();

        [Theory]
        [InlineData(1, 20, true, 1, 20)]
        [InlineData(2, 20, true, 21, 40)]
        [InlineData(3, 20, false, 41, 45)]
        [InlineData(3, 15, false, 31, 45)]   // exactly full last page: no next
        public void Pages(int page, int size, bool hasNext, int start, int end)
        {
            ISlice<int> slice = Items.ToSlice(page, size);
            Assert.Equal(Enumerable.Range(start, end - start + 1), slice);
            Assert.Equal(hasNext, slice.HasNextPage);
            Assert.Equal(page > 1, slice.HasPreviousPage);
            Assert.Equal(start, slice.StartItemIndex);
            Assert.Equal(end, slice.EndItemIndex);
        }

        [Fact]
        public void PastTheEnd_IsEmpty_WithPrevious()
        {
            ISlice<int> slice = Items.AsQueryable().ToSlice(99, 20);
            Assert.Empty(slice);
            Assert.False(slice.HasNextPage);
            Assert.True(slice.HasPreviousPage);
            Assert.Equal(0, slice.StartItemIndex);
        }

        [Fact]
        public void HugePage_DoesNotOverflow()
        {
            ISlice<int> slice = Items.AsQueryable().ToSlice(int.MaxValue, 20);
            Assert.Empty(slice);
        }

        [Fact]
        public void PageBelowOne_IsFirst_AndInvalidSizeThrows()
        {
            Assert.Equal(1, Items.ToSlice(-4, 10).CurrentPageIndex);
            Assert.Throws<ArgumentOutOfRangeException>(() => Items.ToSlice(1, 0));
        }

        [Fact]
        public async Task Async_UsesProviderOperator_OnceWithOneExtraRow()
        {
            int? requested = null;
            ISlice<int> slice = await Items.AsQueryable().ToSliceAsync(1, 20, (q, _) =>
            {
                List<int> list = q.ToList();
                requested = list.Count;
                return Task.FromResult(list);
            });
            Assert.Equal(21, requested); // pageSize + 1
            Assert.Equal(20, slice.Count);
            Assert.True(slice.HasNextPage);
        }

        [Fact]
        public void HtmlHelper_SlicePager_ShowsFirstPreviousNextOnly()
        {
            PagerOptions options = new() { DisplayFirstPage = PagerDisplayMode.Always, HasPagerText = true, HasEntriesText = true };
            string html = P.Pager.Mvc.Core.HtmlHelper.SlicePager(null!, Items.ToSlice(2, 20), p => "/x?page=" + p, options).ToString()!;

            Assert.Contains("href=\"/x?page=1\" rel=\"prev\"", html);
            Assert.Contains("href=\"/x?page=3\" rel=\"next\"", html);
            Assert.DoesNotContain("aria-current", html);   // no page numbers
            Assert.DoesNotContain("Page ", html);          // no pager text
            Assert.DoesNotContain("Showing", html);        // no entries text
            Assert.DoesNotContain("&gt;&gt;", html);       // no Last

            string last = P.Pager.Mvc.Core.HtmlHelper.SlicePager(null!, Items.ToSlice(3, 20), p => "/x?page=" + p, PrePagerOptions.Minimal).ToString()!;
            Assert.Contains("<a aria-disabled=\"true\" class=\"pager-link\">&gt;</a>", last);
        }
    }

    public class SliceBlazorTests : BunitContext
    {
        [Fact]
        public void Pagination_WithSlice_ShowsNoNumbersOrLast()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(p => p
                .Add(x => x.Slice, Enumerable.Range(1, 45).ToSlice(2, 20)));

            Assert.Empty(cut.FindAll("[aria-current=page]"));
            Assert.Empty(cut.FindAll("[aria-label='Last page']"));
            Assert.NotNull(cut.Find("button[aria-label='Next page']"));
            Assert.NotNull(cut.Find("button[aria-label='First page']"));
        }
    }

    public class KnownTotalTests
    {
        [Fact]
        public void ToPagerList_WithKnownTotal_DoesNotCount()
        {
            CountingQueryable source = new(Enumerable.Range(1, 45));
            IPager<int> page = source.ToPagerList(3, 20, totalItemCount: 45);
            Assert.Equal(Enumerable.Range(41, 5), page);
            Assert.Equal(0, source.CountCalls);
            Assert.Equal(3, page.TotalPageCount);
        }

        [Fact]
        public async Task ToPagerListAsync_WithKnownTotal()
        {
            IPager<int> page = await Enumerable.Range(1, 45).AsQueryable().ToPagerListAsync(99, 20, 45, (q, _) => Task.FromResult(q.ToList()));
            Assert.Equal(3, page.CurrentPageIndex);
            Assert.Equal(5, page.Count());
        }

        [Fact]
        public void NegativeTotal_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Enumerable.Range(1, 5).AsQueryable().ToPagerList(1, 10, -1));
        }

        /// <summary>Counts how often Count() reaches the provider.</summary>
        private sealed class CountingQueryable(IEnumerable<int> items) : IQueryable<int>
        {
            private readonly IQueryable<int> _inner = items.ToList().AsQueryable();
            public int CountCalls { get; private set; }
            public Type ElementType => typeof(int);
            public System.Linq.Expressions.Expression Expression => _inner.Expression;
            public IQueryProvider Provider => new CountingProvider(this, _inner.Provider);
            public IEnumerator<int> GetEnumerator() => _inner.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class CountingProvider(CountingQueryable owner, IQueryProvider inner) : IQueryProvider
            {
                public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) => inner.CreateQuery(expression);
                public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) => inner.CreateQuery<TElement>(expression);
                public object? Execute(System.Linq.Expressions.Expression expression) => inner.Execute(expression);
                public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
                {
                    if (expression is System.Linq.Expressions.MethodCallExpression { Method.Name: "Count" })
                        owner.CountCalls++;
                    return inner.Execute<TResult>(expression);
                }
            }
        }
    }

    public class PageRequestTests
    {
        [Theory]
        [InlineData(null, null, 1, 10)]
        [InlineData(0, 0, 1, 10)]
        [InlineData(-2, -5, 1, 10)]
        [InlineData(3, 25, 3, 25)]
        [InlineData(3, 5000, 3, 100)]
        public void Defaults_AndClamping(int? page, int? size, int expectedPage, int expectedSize)
        {
            PageRequest request = new() { Page = page, PageSize = size };
            Assert.Equal(expectedPage, request.GetPageIndex());
            Assert.Equal(expectedSize, request.GetPageSize());
        }

        [Fact]
        public void CustomDefaults()
        {
            PageRequest request = new() { PageSize = 500 };
            Assert.Equal(50, request.GetPageSize(defaultPageSize: 20, maxPageSize: 50));
            Assert.Equal(20, new PageRequest().GetPageSize(defaultPageSize: 20, maxPageSize: 50));
        }

        [Fact]
        public void ToPagerList_UsesRequest()
        {
            PageRequest request = new() { Page = 2, PageSize = 1000 };
            IPager<int> page = Enumerable.Range(1, 250).AsQueryable().ToPagerList(request);
            Assert.Equal(100, page.PageSize);
            Assert.Equal(Enumerable.Range(101, 100), page);

            Assert.Equal(Enumerable.Range(11, 10), Enumerable.Range(1, 50).ToList().ToPagerList(new PageRequest { Page = 2 }));
        }

        [Fact]
        public async Task ToPagerListAsync_And_ToSlice_UseRequest()
        {
            PageRequest request = new() { Page = 3, PageSize = 20 };
            IQueryable<int> q = Enumerable.Range(1, 45).AsQueryable();
            IPager<int> page = await q.ToPagerListAsync(request, (x, _) => Task.FromResult(x.Count()), (x, _) => Task.FromResult(x.ToList()));
            Assert.Equal(Enumerable.Range(41, 5), page);
            Assert.False(q.ToSlice(request).HasNextPage);
        }
    }

    public class SortMapTests
    {
        private sealed record Row(int Id, string Name, DateTime Created, string Secret);

        private static readonly List<Row> Rows =
        [
            new(1, "b", new DateTime(2026, 1, 3), "x"),
            new(2, "a", new DateTime(2026, 1, 1), "z"),
            new(3, "b", new DateTime(2026, 1, 2), "y"),
            new(4, "c", new DateTime(2026, 1, 2), "w"),
        ];

        private static readonly SortMap<Row> Sorts = new SortMap<Row>()
            .Add("name", r => r.Name)
            .Add("created", r => r.Created)
            .Default("-created")
            .ThenBy(r => r.Id);

        private static int[] Ids(string? sort, SortMap<Row>? map = null) =>
            Rows.AsQueryable().ApplySort(sort, map ?? Sorts).Select(r => r.Id).ToArray();

        [Fact]
        public void SingleField_AscendingAndDescending_WithTiebreaker()
        {
            Assert.Equal(new[] { 2, 1, 3, 4 }, Ids("name"));
            Assert.Equal(new[] { 4, 1, 3, 2 }, Ids("-name"));
            Assert.Equal(new[] { 2, 1, 3, 4 }, Ids("+name"));
        }

        [Fact]
        public void MultipleFields()
        {
            Assert.Equal(new[] { 2, 3, 1, 4 }, Ids("name,created"));
            Assert.Equal(new[] { 2, 1, 3, 4 }, Ids(" name , -created "));
        }

        [Fact]
        public void CaseInsensitiveNames()
        {
            Assert.Equal(Ids("name"), Ids("NAME"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("secret")]          // not in the allow-list
        [InlineData("-Secret,unknown")]
        [InlineData(",,,")]
        public void InvalidOrMissing_FallsBackToDefault(string? sort)
        {
            Assert.Equal(new[] { 1, 3, 4, 2 }, Ids(sort)); // -created, then Id
        }

        [Fact]
        public void UnknownFieldsAreSkipped_ValidOnesKept()
        {
            Assert.Equal(new[] { 2, 1, 3, 4 }, Ids("secret,name"));
        }

        [Fact]
        public void RepeatedFields_AndMaxFields()
        {
            SortMap<Row> map = new SortMap<Row> { MaxFields = 1 }.Add("name", r => r.Name).Add("created", r => r.Created).ThenBy(r => r.Id);
            Assert.Equal(new[] { 2, 1, 3, 4 }, Ids("name,-created", map)); // second field ignored
            Assert.Equal(new[] { 2, 1, 3, 4 }, Ids("name,-name", map));
        }

        [Fact]
        public void NoSortNoDefault_TiebreakerOnly()
        {
            SortMap<Row> map = new SortMap<Row>().Add("name", r => r.Name).ThenBy(r => r.Id, descending: true);
            Assert.Equal(new[] { 4, 3, 2, 1 }, Ids(null, map));
        }

        [Fact]
        public void InvalidNames_Throw()
        {
            Assert.Throws<ArgumentException>(() => new SortMap<Row>().Add("-name", r => r.Name));
            Assert.Throws<ArgumentException>(() => new SortMap<Row>().Add(" -name", r => r.Name)); // checked after trimming
            Assert.Throws<ArgumentException>(() => new SortMap<Row>().Add("a,b", r => r.Name));
            Assert.Throws<ArgumentException>(() => new SortMap<Row>().Add(" ", r => r.Name));
        }

        [Fact]
        public void Names_ListsAllowList()
        {
            Assert.Equal(new[] { "created", "name" }, Sorts.Names.OrderBy(n => n));
        }
    }

    [Collection(nameof(PagerSettingsCollection))]
    public class SortMapOrderingTests
    {
        private static readonly SortMap<int> Sorts = new SortMap<int>().Add("value", x => x);

        [Fact]
        public void SortedQuery_PassesUnorderedCheck()
        {
            PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Throw;
            try
            {
                IQueryable<int> provider = ProviderQueryable<int>.From(Enumerable.Range(1, 10));
                Assert.Equal(new[] { 10, 9 }, provider.ApplySort("-value", Sorts).ToPagerList(1, 2));
            }
            finally
            {
                PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Ignore;
            }
        }
    }
}
