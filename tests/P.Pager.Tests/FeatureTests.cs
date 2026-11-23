using System.Collections;
using System.Linq.Expressions;

namespace P.Pager.Tests
{
    public class ClampPageSizeTests
    {
        [Theory]
        [InlineData(null, 10)]
        [InlineData(0, 10)]
        [InlineData(-3, 10)]
        [InlineData(25, 25)]
        [InlineData(100, 100)]
        [InlineData(1_000_000, 100)]
        public void Clamps(int? requested, int expected)
        {
            Assert.Equal(expected, PagerExtension.ClampPageSize(requested));
        }

        [Fact]
        public void CustomDefaultAndMax()
        {
            Assert.Equal(20, PagerExtension.ClampPageSize(null, defaultPageSize: 20, maxPageSize: 50));
            Assert.Equal(50, PagerExtension.ClampPageSize(500, defaultPageSize: 20, maxPageSize: 50));
        }

        [Fact]
        public void InvalidLimits_Throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PagerExtension.ClampPageSize(5, maxPageSize: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => PagerExtension.ClampPageSize(5, defaultPageSize: 0));
        }
    }

    public class PagedResultTests
    {
        [Fact]
        public void ToPagedResult_CopiesItemsAndMetadata()
        {
            PagedResult<int> result = Enumerable.Range(1, 45).ToPagerList(3, 20).ToPagedResult();
            Assert.Equal(Enumerable.Range(41, 5), result.Items);
            Assert.Equal(3, result.Metadata.CurrentPageIndex);
            Assert.Equal(20, result.Metadata.PageSize);
            Assert.Equal(45, result.Metadata.TotalItemCount);
            Assert.Equal(3, result.Metadata.TotalPageCount);
            Assert.True(result.Metadata.HasPreviousPage);
            Assert.False(result.Metadata.HasNextPage);
            Assert.Equal(41, result.Metadata.StartItemIndex);
            Assert.Equal(45, result.Metadata.EndItemIndex);
        }
    }

    public class KeysetPagerTests
    {
        private sealed record Row(int Id, string Name, Guid Key, DateTime Created);

        private static readonly List<Row> Rows = Enumerable.Range(1, 25)
            .Select(i => new Row(i, $"name{i:D2}", new Guid(i, 0, 0, new byte[8]), new DateTime(2026, 1, 1).AddDays(i)))
            .Reverse() // source order must not matter
            .ToList();

        [Fact]
        public void WalksAllPages_ByInt()
        {
            List<int> seen = [];
            KeysetPage<Row, int> page = Rows.AsQueryable().ToKeysetPage(r => r.Id, 10);
            seen.AddRange(page.Items.Select(r => r.Id));
            while (page.HasNextPage)
            {
                page = Rows.AsQueryable().ToKeysetPage(r => r.Id, page.NextKey, 10);
                seen.AddRange(page.Items.Select(r => r.Id));
            }
            Assert.Equal(Enumerable.Range(1, 25), seen);
            Assert.Equal(25, page.NextKey);
        }

        [Fact]
        public void Descending()
        {
            KeysetPage<Row, int> first = Rows.AsQueryable().ToKeysetPage(r => r.Id, 10, descending: true);
            Assert.Equal(Enumerable.Range(16, 10).Reverse(), first.Items.Select(r => r.Id));
            KeysetPage<Row, int> second = Rows.AsQueryable().ToKeysetPage(r => r.Id, first.NextKey, 10, descending: true);
            Assert.Equal(Enumerable.Range(6, 10).Reverse(), second.Items.Select(r => r.Id));
            Assert.True(second.HasNextPage);
        }

        [Fact]
        public void StringGuidAndDateTimeKeys()
        {
            Assert.Equal(new[] { 11, 12, 13 }, Rows.AsQueryable().ToKeysetPage(r => r.Name, "name10", 3).Items.Select(r => r.Id));
            Assert.Equal(new[] { 11, 12, 13 }, Rows.AsQueryable().ToKeysetPage(r => r.Key, new Guid(10, 0, 0, new byte[8]), 3).Items.Select(r => r.Id));
            Assert.Equal(new[] { 11, 12, 13 }, Rows.AsQueryable().ToKeysetPage(r => r.Created, new DateTime(2026, 1, 11), 3).Items.Select(r => r.Id));
        }

        [Fact]
        public void LastPage_HasNoNext_EmptyPage_HasDefaultKey()
        {
            KeysetPage<Row, int> last = Rows.AsQueryable().ToKeysetPage(r => r.Id, 20, 10);
            Assert.Equal(new[] { 21, 22, 23, 24, 25 }, last.Items.Select(r => r.Id));
            Assert.False(last.HasNextPage);

            KeysetPage<Row, int> empty = Rows.AsQueryable().ToKeysetPage(r => r.Id, 25, 10);
            Assert.Empty(empty.Items);
            Assert.Equal(0, empty.NextKey);
        }

        [Fact]
        public async Task Async_UsesProviderOperator()
        {
            int calls = 0;
            KeysetPage<Row, int> page = await Rows.AsQueryable().ToKeysetPageAsync(r => r.Id, 5, 10,
                (q, ct) => { calls++; return Task.FromResult(q.ToList()); });
            Assert.Equal(1, calls);
            Assert.Equal(Enumerable.Range(6, 10), page.Items.Select(r => r.Id));
        }

        [Fact]
        public void InvalidPageSize_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Rows.AsQueryable().ToKeysetPage(r => r.Id, 0));
        }
    }

    /// <summary>Tests that change the global <see cref="PagerSettings"/>; kept in one non-parallel collection.</summary>
    [Collection(nameof(PagerSettingsCollection))]
    public class UnorderedQueryTests
    {
        [Fact]
        public void Ignore_IsDefault()
        {
            Assert.Equal(UnorderedQueryBehavior.Ignore, PagerSettings.UnorderedQueryBehavior);
            ProviderQueryable<int>.From(Enumerable.Range(1, 30)).ToPagerList(1, 10);
        }

        [Fact]
        public async Task Throw_ForUnorderedProviderQuery_ButNotForOrderedOrInMemory()
        {
            PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Throw;
            try
            {
                IQueryable<int> unordered = ProviderQueryable<int>.From(Enumerable.Range(1, 30));
                Assert.Throws<InvalidOperationException>(() => unordered.ToPagerList(1, 10));
                await Assert.ThrowsAsync<InvalidOperationException>(() => unordered.ToPagerListAsync(1, 10));
                await Assert.ThrowsAsync<InvalidOperationException>(() => unordered.ToPagerListAsync(1, 10,
                    (q, _) => Task.FromResult(q.Count()), (q, _) => Task.FromResult(q.ToList())));

                Assert.Equal(10, unordered.OrderBy(x => x).ToPagerList(1, 10).Count());
                Assert.Equal(10, unordered.Where(x => x > 0).OrderByDescending(x => x).ThenBy(x => x).ToPagerList(1, 10).Count());
                Assert.Equal(10, Enumerable.Range(1, 30).ToList().AsQueryable().ToPagerList(1, 10).Count());
            }
            finally
            {
                PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Ignore;
            }
        }
    }

    [CollectionDefinition(nameof(PagerSettingsCollection), DisableParallelization = true)]
    public class PagerSettingsCollection { }

    /// <summary>An IQueryable whose provider is not EnumerableQuery, standing in for a database provider.</summary>
    internal sealed class ProviderQueryable<T> : IOrderedQueryable<T>, IQueryProvider
    {
        private readonly IQueryable<T> _inner;
        private ProviderQueryable(IQueryable<T> inner) => _inner = inner;
        public static IQueryable<T> From(IEnumerable<T> items) => new ProviderQueryable<T>(items.AsQueryable());

        public Type ElementType => typeof(T);
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => this;
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException();
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new ProviderQueryable<TElement>(_inner.Provider.CreateQuery<TElement>(expression));
        public object? Execute(Expression expression) => _inner.Provider.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Provider.Execute<TResult>(expression);
    }
}
