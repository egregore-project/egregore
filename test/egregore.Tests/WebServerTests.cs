// Copyright (c) The Egregore Project & Contributors. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

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

        [Theory(Skip = "FIXME: issues with parallel test runs")]
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