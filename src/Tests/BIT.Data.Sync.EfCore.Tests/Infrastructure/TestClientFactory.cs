using Microsoft.AspNetCore.TestHost;
using System;
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


        public HttpClient CreateClient(string name)
        {
            return _TestServer.CreateClient(); 
        }

        public void Dispose()
        {
            _TestServer.Dispose();
        }
    }
}