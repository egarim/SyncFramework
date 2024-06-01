using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BIT.Data.Sync.Client
{
  
    public class SyncFrameworkHttpClient : ISyncFrameworkClient
    {
        private const string PostRequestUri = "/Sync/Push";
        HttpClient _httpClient;
        string requestUri;
        public string ServerNodeId { get; }
        public SyncFrameworkHttpClient(HttpClient httpClient,string serverNodeId)
        {
           
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("NodeId", serverNodeId);
          
            this.ServerNodeId = serverNodeId;
        }
        public SyncFrameworkHttpClient(string BaseAddress, string NodeId):this(new HttpClient() { BaseAddress=new Uri(BaseAddress)},NodeId)
        {
           
        }
        public virtual async Task PushAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken  = default)
        {

            List<Delta> ToSerialize = new List<Delta>();
            foreach (IDelta delta in Deltas)
            {
                ToSerialize.Add(new Delta(delta));
                cancellationToken.ThrowIfCancellationRequested();
            }
            

            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(List<Delta>));
            MemoryStream msObj = new MemoryStream();
            js.WriteObject(msObj, ToSerialize);
            msObj.Position = 0;
            StreamReader sr = new StreamReader(msObj);
            string jsonDeltas = sr.ReadToEnd();

            var data = new StringContent(jsonDeltas, Encoding.UTF8, "application/json");
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugRequests(PostRequestUri);
            }
            await _httpClient.PostAsync(PostRequestUri, data, cancellationToken).ConfigureAwait(false);

        }
        public virtual async Task<List<Delta>> FetchAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            if (startIndex == null)
                startIndex = "";


            var QueryParams = new Dictionary<string, string>();
            QueryParams.Add(nameof(startIndex), startIndex.ToString());
            QueryParams.Add(nameof(identity), identity);
            cancellationToken.ThrowIfCancellationRequested();
            var query = HttpUtility.ParseQueryString("");
            foreach (KeyValuePair<string, string> CurrentParam in QueryParams)
            {
                query[CurrentParam.Key] = CurrentParam.Value;
            }

            requestUri = $"/Sync/Fetch?{query}";
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugRequests(requestUri);
            }
            var response = await _httpClient.GetStringAsync(requestUri).ConfigureAwait(false);

            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(response)))
            {

                DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(List<Delta>));
                List<Delta> Deltas = (List<Delta>)deserialized.ReadObject(ms);

                return Deltas;
            }



        }
        void DebugRequests(string Uri)
        {
            Debug.WriteLine($"{this._httpClient.BaseAddress}/{Uri}");
        }

    }
}
