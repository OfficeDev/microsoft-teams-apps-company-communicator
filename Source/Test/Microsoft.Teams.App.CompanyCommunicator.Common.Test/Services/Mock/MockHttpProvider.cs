// <copyright file="MockHttpProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.App.CompanyCommunicator.Common.Test.Services.Mock
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Mocking Http provider.
    /// </summary>
    public class MockHttpProvider : IHttpProvider
    {
        /// <inheritdoc/>
        public ISerializer Serializer { get; } = new Serializer();

        /// <inheritdoc/>
        public TimeSpan OverallTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Gets or sets response mapping with key, resposne.
        /// </summary>
        public Dictionary<string, object> Responses { get; set; } = new Dictionary<string, object>();

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            string key = request.Method.ToString() + ":" + request.RequestUri.ToString();
            var response = new HttpResponseMessage();
            if (this.Responses.ContainsKey(key) && response.Content == null)
            {
                response.Content = new StringContent(this.Serializer.SerializeObject(this.Responses[key]));
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            return this.SendAsync(request);
        }
    }
}
