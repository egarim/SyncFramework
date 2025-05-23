using BIT.Data.Sync;
using BIT.Data.Sync.Server;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using static NpgsqlTypes.NpgsqlTsQuery;

namespace SyncFramework.Playground.Components
{
    /// <summary>
    /// Custom HTTP message handler that provides synchronization functionality by delegating requests to an ISyncServer.
    /// Handles GET requests for fetching deltas and POST requests for pushing deltas.
    /// Can optionally forward requests to another HTTP client and only proceed locally if the forwarded request succeeds.
    /// </summary>
    public class ProxyHandler : HttpMessageHandler
    {
        private readonly ISyncServer _syncServer;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a new instance of the ProxyHandler class.
        /// </summary>
        /// <param name="syncServer">The sync server implementation to handle sync operations</param>
        /// <param name="httpClient">Optional HTTP client for forwarding requests</param>
        public ProxyHandler(ISyncServer syncServer, HttpClient httpClient = null)
        {
            _syncServer = syncServer ?? throw new ArgumentNullException(nameof(syncServer));
            _httpClient = httpClient;
        }

        /// <summary>
        /// Processes the HTTP request and returns an HTTP response.
        /// If an HttpClient is provided, forwards the request to it and only proceeds locally if the forwarded request succeeds.
        /// </summary>
        /// <param name="request">The HTTP request message to process</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>The HTTP response message to send back</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Forward the request if an HttpClient is available
            if (_httpClient != null)
            {
                // Create a clone of the request to send
                var forwardedRequest = new HttpRequestMessage
                {
                    Method = request.Method,
                    RequestUri = request.RequestUri,
                    Content = request.Content
                };

                //var currentNodeId = request.Headers.Contains("NodeId") ? request.Headers.GetValues("NodeId").FirstOrDefault() : null;
                //Console.WriteLine($"Current NodeId: {currentNodeId}");
                //Debug.WriteLine($"Current NodeId: {currentNodeId}");
                //forwardedRequest.Headers.Add("NodeId", currentNodeId);


                // Copy all headers
                foreach (var header in request.Headers)
                {
                    forwardedRequest.Headers.Add(header.Key, request.Headers.GetValues(header.Key).FirstOrDefault());
                }

                // Send the request and wait for response
                var forwardedResponse = await _httpClient.SendAsync(forwardedRequest, cancellationToken);

                // If the forwarded request was not successful, return its response immediately
                if (!forwardedResponse.IsSuccessStatusCode)
                {
                    return forwardedResponse;
                }

                // Otherwise continue with local processing
            }

            // Process the request locally
            HttpResponseMessage responseMessage;

            switch (request.Method.Method)
            {
                case "GET":
                    var queryParams = ParseQueryString(request.RequestUri.Query);

                    // Extract query parameters for delta fetching
                    string startIndex = queryParams.TryGetValue("startIndex", out var index) ? index : string.Empty;
                    string identity = queryParams.TryGetValue("identity", out var id) ? id : string.Empty;

                    var output = await ProcessFetchAsync(startIndex, identity, request, cancellationToken);
                    responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(output)
                    };
                    break;

                case "POST":
                    await ProcessPushAsync(request, cancellationToken);
                    responseMessage = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent("Request processed successfully")
                    };
                    break;

                default:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("This method is not supported.")
                    };
                    break;
            }

            return responseMessage;
        }

        /// <summary>
        /// Processes a push request to save deltas to the sync server.
        /// </summary>
        /// <param name="request">The HTTP request containing deltas to push</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        public async Task ProcessPushAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string nodeId = GetHeader("NodeId", request);
            var stream = new StreamReader(await request.Content.ReadAsStreamAsync(cancellationToken));
            var body = await stream.ReadToEndAsync();

            using var ms = new MemoryStream(Encoding.Unicode.GetBytes(body));
            var serializer = new DataContractJsonSerializer(typeof(List<Delta>));
            var deltas = (List<Delta>)serializer.ReadObject(ms);

            await _syncServer.SaveDeltasAsync(nodeId, deltas, cancellationToken);

            var message = $"Push to node:{nodeId}{Environment.NewLine}" +
                         $"Deltas Received:{deltas.Count}{Environment.NewLine}" +
                         $"Identity:{deltas.FirstOrDefault()?.Identity}";
            Debug.WriteLine(message);
        }

        /// <summary>
        /// Processes a fetch request to retrieve deltas from the sync server.
        /// </summary>
        /// <param name="startIndex">The starting index from which to fetch deltas</param>
        /// <param name="identity">The identity of the client requesting deltas</param>
        /// <param name="request">The HTTP request message</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation</param>
        /// <returns>JSON string containing serialized deltas</returns>
        public virtual async Task<string> ProcessFetchAsync(string startIndex, string identity, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string nodeId = GetHeader("NodeId", request);

            var message = $"Fetch from node:{nodeId}{Environment.NewLine}" +
                         $"Start delta index:{startIndex}{Environment.NewLine}" +
                         $"Client identity:{identity}";
            Debug.WriteLine(message);

            // Get deltas based on parameters
            IEnumerable<IDelta> deltaResults;
            startIndex ??= string.Empty;

            if (string.IsNullOrEmpty(identity))
                deltaResults = await _syncServer.GetDeltasAsync(nodeId, startIndex, cancellationToken);
            else
                deltaResults = await _syncServer.GetDeltasFromOtherNodes(nodeId, startIndex, identity, cancellationToken);

            // Convert and serialize deltas
            var deltasToSerialize = deltaResults.Select(d => new Delta(d)).ToList();

            using var msObj = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(List<Delta>));
            serializer.WriteObject(msObj, deltasToSerialize);
            msObj.Position = 0;

            using var sr = new StreamReader(msObj);
            return await sr.ReadToEndAsync();
        }

        /// <summary>
        /// Extracts a specific header value from the HTTP request.
        /// </summary>
        /// <param name="headerName">The name of the header to retrieve</param>
        /// <param name="request">The HTTP request containing the headers</param>
        /// <returns>The header value or null if not found</returns>
        protected string GetHeader(string headerName, HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues(headerName, out var values))
                return values.FirstOrDefault();
            return null;
        }

        /// <summary>
        /// Parses a query string into a dictionary of key-value pairs.
        /// </summary>
        /// <param name="queryString">The query string to parse</param>
        /// <returns>Dictionary containing the parsed query parameters</returns>
        public static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var queryDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(queryString))
                return queryDictionary;

            queryString = queryString.TrimStart('?');
            var pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    queryDictionary[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
                }
            }

            return queryDictionary;
        }
    }
}