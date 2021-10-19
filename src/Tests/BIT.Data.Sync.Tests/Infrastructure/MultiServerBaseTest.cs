using BIT.Data.Sync.Tests.Startups;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using NUnit.Framework;
using System.IO;

namespace BIT.Data.Sync.Tests.Infrastructure
{
    public class MultiServerBaseTest
    {




        [SetUp]
        public virtual void Setup()
        {



        }

        public TestClientFactory GetTestClientFactory()
        {
            Microsoft.AspNetCore.TestHost.TestServer _testServer;
            var hostBuilder = new WebHostBuilder();

            var Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();


            hostBuilder.UseConfiguration(Configuration);
            hostBuilder.UseStartup<TestStartup>();
            _testServer = new Microsoft.AspNetCore.TestHost.TestServer(hostBuilder);
            hostBuilder.ConfigureLogging(logging =>
            {

                logging.ClearProviders();
                //logging.AddConsole();
                logging.AddDebug();

            });
            var testClient = _testServer.CreateClient();
            var testServerHttpClientFactory = new TestClientFactory(testClient);
            return testServerHttpClientFactory;
        }


        protected TestClientFactory TestServerClientFactory
        {
            get
            {
                return GetTestClientFactory();

            }

        }
    }
}