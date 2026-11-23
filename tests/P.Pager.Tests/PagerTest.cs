namespace P.Pager.Tests
{
    public class PagerTest
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        [Fact]
        public void StartPageTest()
        {
            IPager<int> page = Items.ToPagerList(1, 20);
            Assert.Equal(Enumerable.Range(1, 20), page);
            Assert.True(page.IsFirstPage);
            Assert.False(page.HasPreviousPage);
        }

        [Fact]
        public void MiddlePageTest()
        {
            IPager<int> page = Items.ToPagerList(2, 20);
            Assert.Equal(Enumerable.Range(21, 20), page);
            Assert.True(page.HasPreviousPage);
            Assert.True(page.HasNextPage);
        }

        [Fact]
        public void LastPageTest()
        {
            IPager<int> page = Items.ToPagerList(10, 20);
            Assert.Equal(Enumerable.Range(181, 20), page);
            Assert.True(page.IsLastPage);
            Assert.False(page.HasNextPage);
        }

        [Fact]
        public void InvalidPageTest()
        {
            // totalPageCount is 10 but index 12 is requested: the last page is returned.
            IPager<int> page = Items.ToPagerList(12, 20);
            Assert.Equal(10, page.CurrentPageIndex);
            Assert.Equal(Enumerable.Range(181, 20), page);
        }

        [Fact]
        public void NextPageCheck()
        {
            IPager<int> page = Items.ToPagerList(1, 20);
            Assert.True(page.HasNextPage);
        }

        [Fact]
        public void Metadata()
        {
            IPager<int> page = Items.ToPagerList(3, 20);
            Assert.Equal(200, page.TotalItemCount);
            Assert.Equal(10, page.TotalPageCount);
            Assert.Equal(20, page.PageSize);
            Assert.Equal(41, page.StartItemIndex);
            Assert.Equal(60, page.EndItemIndex);
            Assert.Equal(41, page[0]);
        }

        [Fact]
        public void DefaultArguments_FirstPageOfTen()
        {
            IPager<int> page = Items.ToPagerList();
            Assert.Equal(Enumerable.Range(1, 10), page);
        }
    }
}
