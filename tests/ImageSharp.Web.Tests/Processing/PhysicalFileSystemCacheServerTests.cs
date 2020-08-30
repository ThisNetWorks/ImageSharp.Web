// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Web.Tests.TestUtilities;
using Xunit;

namespace SixLabors.ImageSharp.Web.Tests.Processing
{
    public class PhysicalFileSystemCacheServerTests : ServerTestBase<PhysicalFileSystemCacheTestServerFixture>
    {
        private const int Width = 20;
        private static readonly string Command = "?width=" + Width + "&v=" + Guid.NewGuid().ToString();

        public PhysicalFileSystemCacheServerTests(PhysicalFileSystemCacheTestServerFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData(TestConstants.PhysicalTestImage)]
        [InlineData(TestConstants.AzureTestImage)]
        public async Task CanProcessAndResolveImage(string url)
        {
            string ext = Path.GetExtension(url);
            IImageFormat format = Configuration.Default.ImageFormatsManager.FindFormatByFileExtension(ext);

            // First response
            HttpResponseMessage response = await this.HttpClient.GetAsync(url + Command);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentLength > 0);
            Assert.Equal(format.DefaultMimeType, response.Content.Headers.ContentType.MediaType);

            (Image Image, IImageFormat Format) actual = await Image.LoadWithFormatAsync(await response.Content.ReadAsStreamAsync());
            using Image image = actual.Image;

            Assert.Equal(Width, image.Width);
            Assert.Equal(format, actual.Format);

            // Cached Response
            response = await this.HttpClient.GetAsync(url + Command);

            Assert.NotNull(response);
            Assert.True(response.IsSuccessStatusCode);
            Assert.True(response.Content.Headers.ContentLength > 0);
            Assert.Equal(format.DefaultMimeType, response.Content.Headers.ContentType.MediaType);

            (Image Image, IImageFormat Format) cachedActual = await Image.LoadWithFormatAsync(await response.Content.ReadAsStreamAsync());
            using Image cached = cachedActual.Image;

            Assert.Equal(Width, cached.Width);
            Assert.Equal(format, actual.Format);

            // 304 response
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url + Command),
                Method = HttpMethod.Get,
            };

            request.Headers.IfModifiedSince = DateTimeOffset.UtcNow;

            response = await this.HttpClient.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Equal(0, response.Content.Headers.ContentLength);
            Assert.Equal(format.DefaultMimeType, response.Content.Headers.ContentType.MediaType);
        }
    }
}
