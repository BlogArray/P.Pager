namespace P.Pager.Tests
{
    public class StylesheetTests
    {
        private static string RepoFile(string relative)
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "P.Pager.sln")))
                dir = dir.Parent;
            Assert.NotNull(dir);
            return Path.Combine(dir!.FullName, relative);
        }

        [Fact]
        public void MvcAndBlazorShipTheSameStylesheet()
        {
            string blazor = File.ReadAllText(RepoFile("src/P.Pager.Blazor/wwwroot/pager.css"));
            string mvc = File.ReadAllText(RepoFile("src/P.Pager.Mvc.Core/wwwroot/pager.css"));
            Assert.Equal(blazor, mvc);
        }

        [Fact]
        public void StylesheetStylesEveryDefaultThemeClass()
        {
            string css = File.ReadAllText(RepoFile("src/P.Pager.Blazor/wwwroot/pager.css"));
            PagerTheme theme = PagerTheme.Neutral;
            foreach (string? cls in new[] { theme.Container, theme.List, theme.Item, theme.Link, theme.ActiveItem, theme.DisabledItem })
                Assert.Contains("." + cls, css);
        }
    }
}
