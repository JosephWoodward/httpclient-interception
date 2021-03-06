// Copyright (c) Just Eat, 2017. All rights reserved.
// Licensed under the Apache 2.0 license. See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using JustEat.HttpClientInterception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace SampleApp.Tests
{
    public class HttpServerFixture : WebApplicationFactory<Startup>
    {
        public HttpServerFixture()
            : base()
        {
            Interceptor = new HttpClientInterceptorOptions() { ThrowOnMissingRegistration = true };
        }

        public HttpClientInterceptorOptions Interceptor { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Configure filter that makes JustEat.HttpClientInterception available
            builder.ConfigureServices(
                (services) => services.AddSingleton<IHttpMessageHandlerBuilderFilter, InterceptionFilter>(
                    (_) => new InterceptionFilter(Interceptor)));

            // Add the test configuration file to override the application configuration
            string directory = Path.GetDirectoryName(typeof(HttpServerFixture).Assembly.Location);
            string fullPath = Path.Combine(directory, "testsettings.json");

            builder.ConfigureAppConfiguration(
                (_, config) => config.AddJsonFile(fullPath));
        }

        /// <summary>
        /// A class that registers an intercepting HTTP message handler at the end of
        /// the message handler pipeline when an <see cref="HttpClient"/> is created.
        /// </summary>
        private sealed class InterceptionFilter : IHttpMessageHandlerBuilderFilter
        {
            private readonly HttpClientInterceptorOptions _options;

            internal InterceptionFilter(HttpClientInterceptorOptions options)
            {
                _options = options;
            }

            /// <inheritdoc/>
            public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
            {
                return (builder) =>
                {
                    // Run any actions the application has configured for itself
                    next(builder);

                    // Add the interceptor as the last message handler
                    builder.AdditionalHandlers.Add(_options.CreateHttpMessageHandler());
                };
            }
        }
    }
}
