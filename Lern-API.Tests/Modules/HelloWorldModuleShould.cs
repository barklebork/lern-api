﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Lern_API.Tests.Attributes;
using Lern_API.Utilities;
using Nancy;
using Nancy.Testing;
using PetaPoco;
using Xunit;

namespace Lern_API.Tests.Modules
{
    [ExcludeFromCodeCoverage]
    public class HelloWorldModuleShould
    {
        [Theory]
        [AutoMoqData]
        public async Task Return_Status_OK(ILogger logger, IDatabase database)
        {
            var browser = new Browser(new LernBootstrapper(logger, database));

            var result = await browser.Get("/api/hello", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [Theory]
        [AutoMoqData]
        public async Task Return_Hello_World(ILogger logger, IDatabase database)
        {
            var browser = new Browser(new LernBootstrapper(logger, database));

            var result = await browser.Get("/api/hello", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal("Hello, world!", result.Body.AsString());
        }

        [Theory]
        [AutoMoqData]
        public async Task Return_Html_On_False_Route(ILogger logger, IDatabase database)
        {
            var browser = new Browser(new LernBootstrapper(logger, database));

            var result = await browser.Get("/api/false_route", with =>
            {
                with.HttpRequest();
            });

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            Assert.Equal("text/html", result.ContentType);
        }

        [Theory]
        [AutoMoqData]
        public async Task Has_Gzip_Compression(ILogger logger, IDatabase database)
        {
            Environment.SetEnvironmentVariable("GZIP_MINIMUM_BYTES", "0");
            Environment.SetEnvironmentVariable("GZIP_SUPPORTED_MIME_TYPES", "text/plain");

            var browser = new Browser(new LernBootstrapper(logger, database), d => d.Header("Accept-Encoding", "gzip"));

            var result = await browser.Get("/api/hello", with =>
            {
                with.HttpRequest();
            });

            Assert.Contains("Content-Encoding", result.Headers);
            Assert.Equal("gzip", result.Headers["Content-Encoding"]);

            var stream = new GZipStream(result.Body.AsStream(), CompressionMode.Decompress);
            using var reader = new StreamReader(stream);

            Assert.Equal("Hello, world!", await reader.ReadToEndAsync());
        }

        [Theory]
        [AutoMoqData]
        public async Task Use_Gzip_Only_When_OK(ILogger logger, IDatabase database)
        {
            Environment.SetEnvironmentVariable("GZIP_MINIMUM_BYTES", "0");
            Environment.SetEnvironmentVariable("GZIP_SUPPORTED_MIME_TYPES", "text/plain");

            var browser = new Browser(new LernBootstrapper(logger, database), d => d.Header("Accept-Encoding", "gzip"));

            var result = await browser.Get("/api/false_route", with =>
            {
                with.HttpRequest();
            });

            Assert.DoesNotContain("Content-Encoding", result.Headers);

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                var stream = new GZipStream(result.Body.AsStream(), CompressionMode.Decompress);
                using var reader = new StreamReader(stream);

                await reader.ReadToEndAsync();
            });
        }

        [Theory]
        [AutoMoqData]
        public async Task Use_Gzip_Only_When_Configured(ILogger logger, IDatabase database)
        {
            Environment.SetEnvironmentVariable("GZIP_MINIMUM_BYTES", "0");
            Environment.SetEnvironmentVariable("GZIP_SUPPORTED_MIME_TYPES", "");

            var browser = new Browser(new LernBootstrapper(logger, database), d => d.Header("Accept-Encoding", "gzip"));

            var result = await browser.Get("/api/hello", with =>
            {
                with.HttpRequest();
            });

            Assert.DoesNotContain("Content-Encoding", result.Headers);

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                var stream = new GZipStream(result.Body.AsStream(), CompressionMode.Decompress);
                using var reader = new StreamReader(stream);

                await reader.ReadToEndAsync();
            });
        }

        [Theory]
        [AutoMoqData]
        public async Task Use_Gzip_Only_When_Client_Can(ILogger logger, IDatabase database)
        {
            Environment.SetEnvironmentVariable("GZIP_MINIMUM_BYTES", "0");
            Environment.SetEnvironmentVariable("GZIP_SUPPORTED_MIME_TYPES", "text/plain");

            var browser = new Browser(new LernBootstrapper(logger, database));

            var result = await browser.Get("/api/hello", with =>
            {
                with.HttpRequest();
            });

            Assert.DoesNotContain("Content-Encoding", result.Headers);

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                var stream = new GZipStream(result.Body.AsStream(), CompressionMode.Decompress);
                using var reader = new StreamReader(stream);

                await reader.ReadToEndAsync();
            });
        }
    }
}
