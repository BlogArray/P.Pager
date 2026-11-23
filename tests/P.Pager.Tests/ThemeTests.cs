using Bunit;
using P.Pager.Blazor;
using P.Pager.Mvc.Core;

namespace P.Pager.Tests
{
    public class ThemeTests
    {
        private static readonly List<int> Items = Enumerable.Range(1, 200).ToList();

        private static string Render(IPager pager, PagerOptions options) =>
            P.Pager.Mvc.Core.HtmlHelper.Pager(null!, pager, p => "/x?page=" + p, options).ToString()!;

        [Fact]
        public void MvcDefault_IsThe90Markup()
        {
            string html = Render(Items.ToPagerList(3, 20), new PagerOptions { HasPagerText = true });
            Assert.StartsWith("<div aria-label=\"Pagination\" class=\"container\" role=\"navigation\"><ul class=\"pagination\">", html);
            Assert.Contains("<li class=\"page-item active\"><a aria-current=\"page\" class=\"page-link\">3</a></li>", html);
            Assert.Contains("<li class=\"page-item disabled\"><a class=\"page-link\">Page 3 of 10.</a></li>", html); // text stays <a>
        }

        [Fact]
        public void Neutral_IsFrameworkNeutral_WithSpans()
        {
            var options = new PagerOptions(PagerTheme.Neutral) { HasPagerText = true, HasEntriesText = true, PagesToDisplay = 3 };
            string html = Render(Items.ToPagerList(5, 20), options);
            Assert.StartsWith("<div aria-label=\"Pagination\" class=\"pager\" role=\"navigation\"><ul class=\"pager-list\">", html);
            Assert.Contains("<li class=\"pager-item is-active\"><a aria-current=\"page\" class=\"pager-link\">5</a></li>", html);
            Assert.Contains("<span class=\"pager-link\">Page 5 of 10.</span>", html);
            Assert.Contains("<span class=\"pager-link\">Showing 81 to 100 of 200 entries.</span>", html);
            Assert.Contains("<span class=\"pager-link\">&#8230;</span>", html);
            Assert.DoesNotContain("page-link", html);
            Assert.Equal(PagerTheme.Neutral.Link, PrePagerOptions.Neutral.PageClass);
        }

        [Fact]
        public void BootstrapTheme_DropsContainerClass_UsesSpans()
        {
            string html = Render(Items.ToPagerList(1, 20), new PagerOptions(PagerTheme.Bootstrap) { HasPagerText = true });
            Assert.StartsWith("<div aria-label=\"Pagination\" role=\"navigation\"><ul class=\"pagination\">", html);
            Assert.Contains("<li class=\"page-item active\"><a aria-current=\"page\" class=\"page-link\">1</a></li>", html);
            Assert.Contains("<span class=\"page-link\">Page 1 of 10.</span>", html);
            Assert.Equal("page-link", PrePagerOptions.Bootstrap.PageClass);
        }

        [Fact]
        public void ApplyTheme_KeepsLayoutOfOldPresets()
        {
            PagerOptions options = PrePagerOptions.Minimal.ApplyTheme(PagerTheme.Neutral);
            Assert.False(options.HasIndividualPages);
            Assert.Equal(PagerDisplayMode.Always, options.DisplayNextPage);
            Assert.Equal("pager-link", options.PageClass);

            string html = Render(Items.ToPagerList(1, 20), options);
            Assert.Contains("<li class=\"pager-item is-disabled\"><a aria-disabled=\"true\" class=\"pager-link\">&lt;</a></li>", html);
            Assert.DoesNotContain("aria-current", html);
        }

        [Theory]
        [InlineData("Foundation", "<ul class=\"pagination\">", "<li class=\"current\"><a aria-current=\"page\">1</a></li>")]
        [InlineData("UIkit", "<ul class=\"uk-pagination\">", "<li class=\"uk-active\"><a aria-current=\"page\">1</a></li>")]
        [InlineData("Materialize", "<ul class=\"pagination\">", "<li class=\"waves-effect active\"><a aria-current=\"page\">1</a></li>")]
        [InlineData("Bulma", "<ul class=\"pagination-list\">", "<li><a aria-current=\"page\" class=\"pagination-link is-current\">1</a></li>")]
        public void FrameworkThemes(string name, string list, string active)
        {
            PagerTheme theme = (PagerTheme)typeof(PagerTheme).GetProperty(name)!.GetValue(null)!;
            string html = Render(Items.ToPagerList(1, 20), new PagerOptions(theme));
            Assert.Contains(list, html);
            Assert.Contains(active, html);
            Assert.DoesNotContain("class=\"\"", html);
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            PagerTheme theme = PagerTheme.Bootstrap.Clone();
            theme.List = "pagination justify-content-center";
            Assert.Equal("pagination", PagerTheme.Bootstrap.List);
            Assert.Equal("pagination justify-content-center", new PagerOptions().ApplyTheme(theme).ClassToUl);
        }

        [Fact]
        public void BlazorTheme_Parameter()
        {
            using var ctx = new BunitContext();
            IRenderedComponent<Pagination> cut = ctx.Render<Pagination>(p => p
                .Add(x => x.Pager, Items.ToPagerList(1, 20))
                .Add(x => x.Theme, PagerTheme.Bootstrap)
                .Add(x => x.ListClass, "pagination pagination-sm"));

            Assert.Null(cut.Find("nav").GetAttribute("class"));
            Assert.Equal("pagination pagination-sm", cut.Find("ul").GetAttribute("class"));
            Assert.Equal("page-item active", cut.Find("[aria-current=page]").ParentElement!.GetAttribute("class"));
            Assert.Equal("page-link", cut.Find("[aria-current=page]").GetAttribute("class"));
        }
    }

    /// <summary>Tests that read or change the global default theme; they run in the non-parallel settings collection.</summary>
    [Collection(nameof(PagerSettingsCollection))]
    public class GlobalThemeTests
    {
        [Fact]
        public void BuiltInDefault_IsClassicForMvc_NeutralForBlazor()
        {
            Assert.False(PagerSettings.IsDefaultThemeSet);
            Assert.Equal("container", PagerSettings.DefaultTheme.Container);
            Assert.Equal("page-link", new PagerOptions().PageClass);

            using var ctx = new BunitContext();
            IRenderedComponent<Pagination> cut = ctx.Render<Pagination>(p => p.Add(x => x.Pager, Enumerable.Range(1, 50).ToPagerList(1, 10)));
            Assert.Equal("pager", cut.Find("nav").GetAttribute("class"));
            Assert.Equal("pager-item is-active", cut.Find("[aria-current=page]").ParentElement!.GetAttribute("class"));
        }

        [Fact]
        public void DefaultTheme_AppliesToNewOptions_OldPresets_AndBlazor()
        {
            PagerSettings.DefaultTheme = PagerTheme.Neutral;
            try
            {
                Assert.True(PagerSettings.IsDefaultThemeSet);
                Assert.Equal("pager-link", new PagerOptions().PageClass);
                Assert.Equal("pager-link", PrePagerOptions.ClassicPager.PageClass);

                PagerSettings.DefaultTheme = PagerTheme.Bootstrap;
                using var ctx = new BunitContext();
                IRenderedComponent<Pagination> cut = ctx.Render<Pagination>(p => p.Add(x => x.Pager, Enumerable.Range(1, 50).ToPagerList(1, 10)));
                Assert.Equal("pagination", cut.Find("ul").GetAttribute("class"));
            }
            finally
            {
                PagerSettings.ResetDefaultTheme();
            }
            Assert.False(PagerSettings.IsDefaultThemeSet);
            Assert.Equal("page-link", new PagerOptions().PageClass);
        }

        [Fact]
        public void DefaultTheme_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => PagerSettings.DefaultTheme = null!);
        }
    }
}
