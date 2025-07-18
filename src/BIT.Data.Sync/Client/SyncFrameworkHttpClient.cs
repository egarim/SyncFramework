﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace BIT.Data.Sync.Client
{

    public class SyncFrameworkHttpClient : ISyncFrameworkClient
    {
        private const string PushRequestUri = "Push";
        private const string FetchRequestUri = "Fetch?";
        private const string HandShakeRequestUri = "HandShake";

        HttpClient _httpClient;
        string requestUri;
        public string ServerNodeId { get; }
        public SyncFrameworkHttpClient(HttpClient httpClient, string serverNodeId)
        {

            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("NodeId", serverNodeId);
            this.ServerNodeId = serverNodeId;
        }
        public SyncFrameworkHttpClient(string BaseAddress, string NodeId) : this(new HttpClient() { BaseAddress = new Uri(BaseAddress) }, NodeId)
        {

        }
        public virtual async Task<PushOperationResponse> PushAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken = default)
        {
            PushOperationResponse pushOperationResponse = null;
            try
            {
                HttpResponseMessage httpResponseMessage = await PushCore(Deltas, cancellationToken).ConfigureAwait(false);
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string content = await httpResponseMessage.Content.ReadAsStringAsync();
                    using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(content)))
                    {

                        DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(PushOperationResponse));
                        pushOperationResponse = (PushOperationResponse)deserialized.ReadObject(ms);
                    }


                    //pushOperationResponse= JsonSerializer.Deserialize<PushOperationResponse>(content);

                }
                return pushOperationResponse;
            }
            catch (Exception ex)
            {

                pushOperationResponse = new PushOperationResponse();
                pushOperationResponse.Success = false;
                pushOperationResponse.Message = $"An error occurred while pushing deltas to the server.{ex.Message}";
                return pushOperationResponse;
            }
           
        }

        private async Task<HttpResponseMessage> PushCore(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken)
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
                DebugRequests(PushRequestUri);
            }


            return await _httpClient.PostAsync(PushRequestUri, data, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<FetchOperationResponse> FetchAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            try
            {
                string responseString = await FetchCore(startIndex, identity, FetchRequestUri, cancellationToken).ConfigureAwait(false);

                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(responseString)))
                {


                    DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(FetchOperationResponse));
                    FetchOperationResponse response = (FetchOperationResponse)deserialized.ReadObject(ms);

                    return response;
                }

            }
            catch (Exception ex)
            {

                FetchOperationResponse fetchOperationResponse = new FetchOperationResponse();
                fetchOperationResponse.Success = false;
                fetchOperationResponse.Message = $"An error occurred while fetching deltas from the server: {ex.Message}";
                return fetchOperationResponse;
            }
          


        }

        private async Task<string> FetchCore(string startIndex, string identity, string EndPoint, CancellationToken cancellationToken)
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

            requestUri = $"{EndPoint}{query}";
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugRequests(requestUri);
            }


            var response = await _httpClient.GetStringAsync(requestUri).ConfigureAwait(false);
            return response;
        }


        void DebugRequests(string Uri)
        {
            Debug.WriteLine($"{this._httpClient.BaseAddress}/{Uri}");
        }

        public async Task<FetchOperationResponse> HandShake(CancellationToken cancellationToken)
        {
            try
            {
                string responseString = "";
                responseString = await _httpClient.GetStringAsync(HandShakeRequestUri).ConfigureAwait(false);
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(responseString)))
                {



                    DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(FetchOperationResponse));
                    FetchOperationResponse response = (FetchOperationResponse)deserialized.ReadObject(ms);

                    return response;
                }


            }
            catch (Exception ex)
            {

                FetchOperationResponse fetchOperationResponse = new FetchOperationResponse();
                fetchOperationResponse.Success = false;
                fetchOperationResponse.Message = $"An error occurred while fetching deltas from the server: {ex.Message}";
                return fetchOperationResponse;
            }
        }

       
    }
}
