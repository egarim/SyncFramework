using BIT.Data.Sync.Server;
using System.Net;

namespace SyncFramework.Playground
{
    public class FakeHandler : HttpMessageHandler
    {
        ISyncServer syncServer;
        public FakeHandler(ISyncServer syncServer)
        {
            this.syncServer = syncServer;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage responseMessage;

            switch (request.Method.Method)
            {
                case "GET":
                    responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("Hello! This is a response to a GET request!")
                    };
                    break;
                case "POST":
                    responseMessage = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent("Hello! This is a response to a POST request!")
                    };
                    break;
                // You can add cases for other HTTP methods as needed
                default:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("This method is not supported.")
                    };
                    break;
            }

            return await Task.FromResult(responseMessage);
        }
    }
}
