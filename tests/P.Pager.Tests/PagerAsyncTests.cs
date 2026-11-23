namespace P.Pager.Tests
{
    public class PagerAsyncTests
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        [Fact]
        public async Task StartPageTestAsync()
        {
            IPager<int> page = await Items.ToPagerListAsync(1, 20);
            Assert.Equal(Enumerable.Range(1, 20), page);
        }

        [Fact]
        public async Task MiddlePageTestAsync()
        {
            IPager<int> page = await Items.ToPagerListAsync(2, 20);
            Assert.Equal(Enumerable.Range(21, 20), page);
        }

        [Fact]
        public async Task LastPageTest()
        {
            IPager<int> page = await Items.ToPagerListAsync(10, 20);
            Assert.Equal(Enumerable.Range(181, 20), page);
            Assert.True(page.IsLastPage);
        }

        [Fact]
        public async Task InvalidPageTest()
        {
            IPager<int> page = await Items.ToPagerListAsync(12, 20);
            Assert.Equal(10, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(181, 20), page);
        }

        [Fact]
        public async Task NextPageCheck()
        {
            IPager<int> page = await Items.ToPagerListAsync(1, 20);
            Assert.True(page.HasNextPage);
        }

        [Fact]
        public async Task NullPageIndex_IsFirstPage()
        {
            IPager<int> page = await Items.AsQueryable().ToPagerListAsync(null, 20);
            Assert.Equal(1, page.CurrentPageIndex);
        }

        [Fact]
        public async Task DefaultArguments_FirstPageOfTen()
        {
            Assert.Equal(Enumerable.Range(1, 10), await Items.ToPagerListAsync());
            Assert.Equal(Enumerable.Range(1, 10), await Items.AsQueryable().ToPagerListAsync());
        }

        [Fact]
        public async Task DelegateOverload_UsesProviderOperators_AndClamps()
        {
            int countCalls = 0, listCalls = 0;
            using CancellationTokenSource cts = new();

            IPager<int> page = await Items.AsQueryable().ToPagerListAsync(
                int.MaxValue, 20,
                (q, ct) => { countCalls++; Assert.Equal(cts.Token, ct); return Task.FromResult(q.Count()); },
                (q, ct) => { listCalls++; Assert.Equal(cts.Token, ct); return Task.FromResult(q.ToList()); },
                cts.Token);

            Assert.Equal(1, countCalls);
            Assert.Equal(1, listCalls);
            Assert.Equal(10, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(181, 20), page);
        }

        [Fact]
        public async Task DelegateOverload_ValidatesArguments()
        {
            IQueryable<int> q = Items.AsQueryable();
            Func<IQueryable<int>, CancellationToken, Task<int>> count = (x, _) => Task.FromResult(x.Count());
            Func<IQueryable<int>, CancellationToken, Task<List<int>>> list = (x, _) => Task.FromResult(x.ToList());

            await Assert.ThrowsAsync<ArgumentNullException>(() => q.ToPagerListAsync(1, 10, null!, list));
            await Assert.ThrowsAsync<ArgumentNullException>(() => q.ToPagerListAsync(1, 10, count, null!));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => q.ToPagerListAsync(1, 0, count, list));
        }
    }
}
