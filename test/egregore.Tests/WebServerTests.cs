using System.Threading.Tasks;
using Xunit;

namespace egregore.Tests
{
    public sealed class WebServerTests : IClassFixture<WebServerFactory>
    {
        private readonly WebServerFactory _factory;

        public WebServerTests(WebServerFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/whois", "application/json; charset=utf-8")]
        public async Task Public_endpoints_respond_with_success(string url, string contentType)
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            Assert.Equal(contentType, response.Content.Headers.ContentType.ToString());
        }
    }
}
