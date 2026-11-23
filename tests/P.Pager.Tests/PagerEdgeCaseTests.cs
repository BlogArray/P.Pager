using P.Pager.Mvc.Core;

namespace P.Pager.Tests
{
    public class PagerEdgeCaseTests
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(int.MinValue)]
        public void PageIndexBelowOne_ReturnsFirstPage(int pageIndex)
        {
            IPager<int> page = Items.ToPagerList(pageIndex, 20);
            Assert.Equal(1, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(1, 20), page);
        }

        [Theory]
        [InlineData(11)]
        [InlineData(100_000_000)]   // previously looped ~100M times
        [InlineData(300_000_000)]   // previously overflowed (pageIndex - 1) * pageSize
        [InlineData(int.MaxValue)]
        public void PageIndexPastEnd_ReturnsLastPage(int pageIndex)
        {
            IPager<int> page = Items.ToPagerList(pageIndex, 20);
            Assert.Equal(10, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(181, 20), page);
            Assert.True(page.IsLastPage);
            Assert.False(page.HasNextPage);
        }

        [Fact]
        public async Task PageIndexPastEnd_ReturnsLastPage_Async()
        {
            IPager<int> page = await Items.ToPagerListAsync(int.MaxValue, 20);
            Assert.Equal(10, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(181, 20), page);
        }

        [Fact]
        public void PartialLastPage()
        {
            IPager<int> page = Enumerable.Range(1, 45).ToPagerList(99, 20);
            Assert.Equal(3, page.TotalPageCount);
            Assert.Equal(3, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(41, 5), page);
            Assert.Equal(41, page.StartItemIndex);
            Assert.Equal(45, page.EndItemIndex);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void PageSizeBelowOne_Throws(int pageSize)
        {
            if (pageSize == 0)
            {
                // 9.0 compatibility: 0 returns an empty page instead of throwing.
                IPager<int> page = Items.ToPagerList(1, pageSize);
                Assert.Empty(page);
                Assert.Equal(200, page.TotalItemCount);
                Assert.Empty(Items.AsQueryable().ToPagerList(1, 0));
                return;
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => Items.ToPagerList(1, pageSize));
        }

        [Fact]
        public async Task PageSizeBelowOne_Throws_Async()
        {
            Assert.Empty(await Items.ToPagerListAsync(1, 0));                 // 9.0 compatibility
            Assert.Empty(await Items.AsQueryable().ToPagerListAsync(1, 0));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Items.ToPagerListAsync(1, -1));
            // New APIs require at least 1.
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Items.AsQueryable().ToPagerListAsync(1, 0,
                (q, _) => Task.FromResult(q.Count()), (q, _) => Task.FromResult(q.ToList())));
        }

        [Fact]
        public void EmptySource_IsSingleEmptyFirstAndLastPage()
        {
            IPager<int> page = new List<int>().ToPagerList(5, 10);
            Assert.Empty(page);
            Assert.Equal(1, page.CurrentPageIndex);
            Assert.Equal(0, page.TotalPageCount);
            Assert.True(page.IsFirstPage);
            Assert.True(page.IsLastPage);
            Assert.False(page.HasNextPage);
            Assert.False(page.HasPreviousPage);
            Assert.Equal(0, page.StartItemIndex);
            Assert.Equal(0, page.EndItemIndex);
        }

        [Fact]
        public void Helper_EmptySource_DoesNotLinkToPageZero()
        {
            IPager<int> page = new List<int>().ToPagerList(1, 10);
            string html = HtmlHelper.Pager(null!, page, p => "/items?page=" + p, PrePagerOptions.ClassicPagerWithFirstAndLastPages).ToString();
            Assert.DoesNotContain("page=0", html);
            Assert.DoesNotContain("href", html);
        }

        [Fact]
        public void Helper_HrefIsNotEntityEncodedPerCharacter()
        {
            IPager<int> page = Items.ToPagerList(1, 20);
            string html = HtmlHelper.Pager(null!, page, p => "/items?page=" + p).ToString();
            Assert.Contains("href=\"/items?page=2\"", html);
        }
    }
}
