using BIT.Data.Sync;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Http;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;

namespace SyncFramework.Playground
{
    public class FakeHandler : HttpMessageHandler
    {
        ISyncServer syncServer;
        public FakeHandler(ISyncServer syncServer)
        {
            this.syncServer = syncServer;
        }
        protected string GetHeader(string HeaderName, HttpRequestMessage request)
        {
            var stringValues = request.Headers.FirstOrDefault(h => h.Key == HeaderName);
            return stringValues.Value.FirstOrDefault();
        }
        public async Task ProcessPush(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string NodeId = GetHeader("NodeId", request);
            var stream = new StreamReader(request.Content.ReadAsStream());
            var body = await stream.ReadToEndAsync();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(body)))
            {

                DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(List<Delta>));
                List<Delta> Deltas = (List<Delta>)deserialized.ReadObject(ms);
                await syncServer.SaveDeltasAsync(NodeId, Deltas, new CancellationToken());
                var Message = $"Push to node:{NodeId}{Environment.NewLine}Deltas Received:{Deltas.Count}{Environment.NewLine}Identity:{Deltas.FirstOrDefault()?.Identity}";
                Debug.WriteLine(Message);

            }
        }

        public virtual async Task<string> ProcessFetch(string startIndex, string identity, HttpRequestMessage request, CancellationToken cancellationToken)
        {

            string NodeId = GetHeader("NodeId", request);


            var Message = $"Fetch from node:{NodeId}{Environment.NewLine}Start delta index:{startIndex}{Environment.NewLine}Client identity:{identity}";

            Debug.WriteLine(Message);
            IEnumerable<IDelta> enumerable;

            if (startIndex == null)
                startIndex = "";

            if (string.IsNullOrEmpty(identity))
                enumerable = await syncServer.GetDeltasAsync(NodeId, startIndex, new CancellationToken());
            else
                enumerable = await syncServer.GetDeltasFromOtherNodes(NodeId, startIndex, identity, new CancellationToken());

            List<Delta> toSerialize = new List<Delta>();
            foreach (IDelta delta in enumerable)
            {
                toSerialize.Add(new Delta(delta));
            }
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(List<Delta>));
            MemoryStream msObj = new MemoryStream();
            js.WriteObject(msObj, toSerialize);
            msObj.Position = 0;
            StreamReader sr = new StreamReader(msObj);
            string jsonDeltas = sr.ReadToEnd();
            return jsonDeltas;

        }
        private static Dictionary<string, string> GetQueryParameters(Uri uri)
        {
            return HttpUtility.ParseQueryString(uri.Query)
                .AllKeys
                .ToDictionary(key => key, key => HttpUtility.ParseQueryString(uri.Query)[key]);
        }
        public static Dictionary<string, string> Parse(string queryString)
        {
            var queryDictionary = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(queryString))
            {
                queryString = queryString.StartsWith('?') ? queryString.Substring(1) : queryString;
                var pairs = queryString.Split('&');

                foreach (var pair in pairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        queryDictionary[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
                    }
                }
            }

            return queryDictionary;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage responseMessage;

            switch (request.Method.Method)
            {
                case "GET":
                    var Values=Parse(request.RequestUri.Query);
                    //NameValueCollection Values = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);

                    //var queryParams = GetQueryParameters(request.RequestUri);
                    //foreach (var param in queryParams)
                    //{
                    //    Debug.WriteLine($"{param.Key} = {param.Value}");
                    //}

                    string startIndex = Values.FirstOrDefault().Value.ToString();
                    string identity = Values.LastOrDefault().Value.ToString();
                    //startIndex = Values.Get("startIndex");
                    //identity = Values.Get("identity");
                    var output=await ProcessFetch(startIndex, identity, request, cancellationToken);
                    responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(output)
                    };
                    break;
                case "POST":
                    await ProcessPush(request, cancellationToken);

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
