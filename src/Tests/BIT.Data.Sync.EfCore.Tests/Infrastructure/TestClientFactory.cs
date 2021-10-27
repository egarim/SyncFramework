using Microsoft.AspNetCore.TestHost;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BIT.Data.Sync.EfCore.Tests.Infrastructure
{
    public class TestClientFactory : IHttpClientFactory
    {
        TestServer _TestServer;

        public TestClientFactory(TestServer TestServer)
        {
            _TestServer = TestServer;
        }

        public TimeSpan Timeout { get; set; }

        Dictionary<string, HttpClient> Clients = new Dictionary<string, HttpClient>();
        public HttpClient CreateClient(string name)
        {
            if (Clients.ContainsKey(name))
                return Clients[name];

            HttpClient NewClient = _TestServer.CreateClient();
            Clients.Add(name, NewClient);

            return NewClient;
        }

        public void Dispose()
        {
            _TestServer.Dispose();
        }
    }
}