using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace SyncFramework.ConsoleApp.Services
{
    public class HttpClientFactory : IHttpClientFactory
    {
        private Dictionary<string, HttpClient> _clients = new();

        public HttpClient CreateClient(string name)
        {
            if (_clients.ContainsKey(name))
                return _clients[name];

            HttpClient newClient = new HttpClient() { BaseAddress = new Uri("localhost") };
            _clients.Add(name, newClient);
            return newClient;
        }
    }
}
