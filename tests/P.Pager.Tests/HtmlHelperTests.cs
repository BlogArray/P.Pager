using P.Pager.Mvc.Core;

namespace P.Pager.Tests
{
    public class HtmlHelperTests
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        private static string Render(IPager pager, PagerOptions? options = null) =>
            HtmlHelper.Pager(null!, pager, p => "/items?page=" + p, options ?? new PagerOptions()).ToString()!;

        [Fact]
        public void Container_IsLabelledNavigationLandmark_AndKeeps90Class()
        {
            string html = Render(Items.ToPagerList(1, 20));
            Assert.StartsWith("<div aria-label=\"Pagination\" class=\"container\" role=\"navigation\">", html);
        }

        [Fact]
        public void AriaLabel_CanBeOmitted()
        {
            string html = Render(Items.ToPagerList(1, 20), new PagerOptions { AriaLabel = null, ClassToPagerContainer = null });
            Assert.StartsWith("<div role=\"navigation\">", html);
        }

        [Fact]
        public void CurrentPage_HasAriaCurrent_AndNoHref()
        {
            string html = Render(Items.ToPagerList(3, 20));
            Assert.Contains("<a aria-current=\"page\" class=\"page-link\">3</a>", html);
            Assert.DoesNotContain("href=\"/items?page=3\"", html);
        }

        [Fact]
        public void DisabledLinks_AreAriaDisabled_WithoutRel()
        {
            string html = Render(Items.ToPagerList(1, 20), PrePagerOptions.ClassicPagerWithFirstAndLastPages);
            Assert.Contains("<a aria-disabled=\"true\" class=\"page-link\">&lt;</a>", html);
            Assert.DoesNotContain("rel=\"prev\"", html);
            Assert.Contains("<a class=\"page-link\" href=\"/items?page=2\" rel=\"next\">&gt;</a>", html);
        }

        [Fact]
        public void DefaultLabels_AreValidHtml()
        {
            string html = Render(Items.ToPagerList(5, 20), PrePagerOptions.ClassicPagerWithFirstAndLastPages);
            Assert.Contains(">&lt;&lt;</a>", html);
            Assert.Contains(">&gt;&gt;</a>", html);
            Assert.DoesNotContain("><<", html);
        }

        [Fact]
        public void LabelsWithBraces_DoNotThrow()
        {
            PagerOptions options = new()
            {
                TextToNextPage = "<svg style=\"x{fill:red}\"></svg>",
                TextToPreviousPage = "Prev {0}",
                PagerTextFormat = "{bad",
                HasPagerText = true,
            };
            string html = Render(Items.ToPagerList(3, 20), options);
            Assert.Contains("<svg style=\"x{fill:red}\"></svg>", html);
            Assert.Contains("Prev 2", html);
            Assert.Contains("{bad", html);
        }

        [Fact]
        public void NullLabels_DoNotThrow()
        {
            PagerOptions options = new() { TextToNextPage = null!, TextForDelimiter = null };
            Render(Items.ToPagerList(3, 20), options);
        }
    }
}
