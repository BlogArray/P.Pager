using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace P.Pager.Tests
{
    /// <summary>Checks that <see cref="PageRequest"/> binds from the query string in a real ASP.NET Core pipeline.</summary>
    public sealed class PageRequestBindingTests : IAsyncLifetime
    {
        private WebApplication _app = null!;
        private HttpClient _client = null!;

        public async Task InitializeAsync()
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddControllers().AddApplicationPart(typeof(PageRequestBindingTests).Assembly);
            _app = builder.Build();

            _app.MapGet("/minimal", ([AsParameters] PageRequest request) =>
                Enumerable.Range(1, 250).AsQueryable().ApplySort(request.Sort, Sorts).ToPagerList(request).ToPagedResult());
            _app.MapControllers();

            await _app.StartAsync();
            _client = _app.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            await _app.DisposeAsync();
        }

        internal static readonly SortMap<int> Sorts = new SortMap<int>().Add("value", x => x).ThenBy(x => x);

        [Theory]
        [InlineData("/minimal")]
        [InlineData("/mvc")]
        public async Task Binds_PageSizeAndSort(string path)
        {
            PagedResult<int>? result = await _client.GetFromJsonAsync<PagedResult<int>>($"{path}?page=2&pageSize=5&sort=-value");
            Assert.NotNull(result);
            Assert.Equal(new[] { 245, 244, 243, 242, 241 }, result!.Items);
            Assert.Equal(2, result.Metadata.CurrentPageIndex);
            Assert.Equal(5, result.Metadata.PageSize);
        }

        [Theory]
        [InlineData("/minimal")]
        [InlineData("/mvc")]
        public async Task MissingValues_UseDefaults_AndHugeSizeIsCapped(string path)
        {
            PagedResult<int>? defaults = await _client.GetFromJsonAsync<PagedResult<int>>(path);
            Assert.Equal(1, defaults!.Metadata.CurrentPageIndex);
            Assert.Equal(10, defaults.Metadata.PageSize);

            PagedResult<int>? capped = await _client.GetFromJsonAsync<PagedResult<int>>($"{path}?pageSize=1000000");
            Assert.Equal(100, capped!.Metadata.PageSize);
        }

        [Fact]
        public async Task MinimalApi_RejectsNonNumericPage()
        {
            HttpResponseMessage response = await _client.GetAsync("/minimal?page=abc");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [ApiController]
    [Route("mvc")]
    public sealed class PageRequestTestController : ControllerBase
    {
        [HttpGet]
        public PagedResult<int> Get([FromQuery] PageRequest request) =>
            Enumerable.Range(1, 250).AsQueryable().ApplySort(request.Sort, PageRequestBindingTests.Sorts).ToPagerList(request).ToPagedResult();
    }
}
