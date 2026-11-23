using Bunit;
using P.Pager.Blazor;

namespace P.Pager.Tests
{
    public class BlazorPaginationTests : BunitContext
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        [Fact]
        public void RendersButtons_AndRaisesOnPageChanged()
        {
            int? clicked = null;
            IRenderedComponent<Pagination> cut = Render<Pagination>(p => p
                .Add(x => x.Pager, Items.ToPagerList(3, 20))
                .Add(x => x.OnPageChanged, page => clicked = page));

            Assert.Equal("3", cut.Find("[aria-current=page]").TextContent);
            Assert.Equal(5, cut.FindAll("li.pager-item:not(.is-disabled) button").Count(b => int.TryParse(b.TextContent, out _)) + 1);

            cut.Find("button[aria-label='Next page']").Click();
            Assert.Equal(4, clicked);
        }

        [Fact]
        public void PageUrl_RendersLinks()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(p => p
                .Add(x => x.Pager, Items.ToPagerList(1, 20))
                .Add(x => x.PageUrl, page => $"/members?page={page}"));

            Assert.Equal("/members?page=2", cut.Find("a[rel=next]").GetAttribute("href"));
            Assert.Empty(cut.FindAll("a[rel=prev]"));
            Assert.Equal("true", cut.Find("li.is-disabled span[aria-label='Previous page']").GetAttribute("aria-disabled"));
            Assert.Equal("Pagination", cut.Find("nav").GetAttribute("aria-label"));
        }

        [Fact]
        public void HideWhenSinglePage()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(p => p
                .Add(x => x.Pager, Enumerable.Range(1, 5).ToPagerList(1, 20))
                .Add(x => x.HideWhenSinglePage, true));
            Assert.Equal(string.Empty, cut.Markup.Trim());
        }

        [Fact]
        public void WindowAtEnd_ShowsLastPages()
        {
            IRenderedComponent<Pagination> cut = Render<Pagination>(p => p
                .Add(x => x.Pager, Items.ToPagerList(10, 20)));
            string[] numbers = cut.FindAll("li").Select(li => li.TextContent.Trim()).Where(t => int.TryParse(t, out _)).ToArray();
            Assert.Equal(new[] { "6", "7", "8", "9", "10" }, numbers);
        }
    }
}
