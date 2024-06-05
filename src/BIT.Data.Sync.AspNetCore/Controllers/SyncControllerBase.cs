
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BIT.Data.Sync.Client;
using System.Text.RegularExpressions;


namespace BIT.Data.Sync.AspNetCore.Controllers
{
    
    public abstract class SyncControllerBase : ControllerBase
    {


        protected readonly ILogger<SyncControllerBase> _logger;
        protected readonly ISyncServer _SyncServer;
        protected string GetHeader(string HeaderName)
        {
            Microsoft.Extensions.Primitives.StringValues stringValues = HttpContext.Request.Headers[HeaderName];
            return stringValues;
        }


        public SyncControllerBase(ILogger<SyncControllerBase> logger, ISyncServer SyncServer)
        {
            _logger = logger;
            _SyncServer = SyncServer;
        }
        [HttpPost(nameof(Push))]
        public virtual async Task<string> Push()
        {
            PushOperationResponse pushOperationResponse = new PushOperationResponse();
            try
            {
               
                string NodeId = GetHeader("NodeId");
                var stream = new StreamReader(this.Request.Body);
                var body = await stream.ReadToEndAsync();
                using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(body)))
                {

                    DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(List<Delta>));
                    List<Delta> Deltas = (List<Delta>)deserialized.ReadObject(ms);
                    await _SyncServer.SaveDeltasAsync(NodeId, Deltas, new CancellationToken());
                    var Message = $"Push to node:{NodeId}{Environment.NewLine}Deltas Received:{Deltas.Count}{Environment.NewLine}Identity:{Deltas.FirstOrDefault()?.Identity}";
                    _logger.LogInformation(Message);
                    Debug.WriteLine(Message);
                    pushOperationResponse.Success = true;
                    pushOperationResponse.Message = Message;
                }
            }
            catch (NodeNotFoundException argEx)
            {
                _logger.LogError(argEx, "An argument null exception occurred.");
                pushOperationResponse.Success = false;
                pushOperationResponse.Message = argEx.Message;
            }
            catch (ArgumentNullException argEx)
            {
                _logger.LogError(argEx, "An argument null exception occurred.");
            }
            catch (InvalidOperationException invOpEx)
            {
                _logger.LogError(invOpEx, "An invalid operation exception occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unknown exception occurred.");
            }
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(PushOperationResponse));
            MemoryStream msObj = new MemoryStream();
            js.WriteObject(msObj, pushOperationResponse);
            msObj.Position = 0;
            StreamReader sr = new StreamReader(msObj);
            string ResponseString = sr.ReadToEnd();
            return ResponseString;
          



        }
        [HttpGet("Fetch")]
        public virtual async Task<string> Fetch(string startIndex, string identity)
        {
            FetchOperationResponse fetchOperationResponse = new FetchOperationResponse();
            try
            {
               
                string NodeId = GetHeader("NodeId");


                var Message = $"Fetch from node:{NodeId}{Environment.NewLine}Start delta index:{startIndex}{Environment.NewLine}Client identity:{identity}";
                _logger.LogInformation(Message);
                Debug.WriteLine(Message);
                IEnumerable<IDelta> enumerable;

                if (startIndex == null)
                    startIndex = "";

                if (string.IsNullOrEmpty(identity))
                    enumerable = await _SyncServer.GetDeltasAsync(NodeId, startIndex, new CancellationToken());
                else
                    enumerable = await _SyncServer.GetDeltasFromOtherNodes(NodeId, startIndex, identity, new CancellationToken());

                //List<Delta> toSerialize = new List<Delta>();
                //foreach (IDelta delta in enumerable)
                //{
                //    toSerialize.Add(new Delta(delta));
                //}
                foreach (IDelta delta in enumerable)
                {
                    fetchOperationResponse.Deltas.Add(new Delta(delta));
                }

                fetchOperationResponse.Success = true;
               
            }
            catch (NodeNotFoundException argEx)
            {
                SetException(fetchOperationResponse, argEx, "An argument null exception occurred.");
            }
            catch (ArgumentNullException argEx)
            {
                SetException(fetchOperationResponse, argEx, "An argument null exception occurred.");
            }
            catch (InvalidOperationException argEx)
            {
            
                SetException(fetchOperationResponse, argEx, "An invalid operation exception occurred.");
            }
            catch (Exception ex)
            {
                SetException(fetchOperationResponse, ex, "An unknown exception occurred.");
            }
            string fetchResponse = CreateFetchResponse(fetchOperationResponse);
            return fetchResponse;



        }

        protected virtual void SetException(FetchOperationResponse fetchOperationResponse, Exception argEx, string anargumentnullexceptionoccurred)
        {
            fetchOperationResponse.Success = false;
            fetchOperationResponse.Message = argEx.Message;
            _logger.LogError(argEx, anargumentnullexceptionoccurred);
        }

        protected virtual string CreateFetchResponse(FetchOperationResponse fetchOperationResponse)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(FetchOperationResponse));
            MemoryStream msObj = new MemoryStream();
            js.WriteObject(msObj, fetchOperationResponse);
            msObj.Position = 0;
            StreamReader sr = new StreamReader(msObj);
            string fetchResponse = sr.ReadToEnd();
            return fetchResponse;
        }

        [HttpPost("RegisterNode")]
        public virtual async Task<bool> RegisterNode()
        {
            var stream = new StreamReader(this.Request.Body);
            var body = await stream.ReadToEndAsync();

            RegisterNodeRequest Request = null;


            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(body)))
            {
                DataContractJsonSerializer deserialized = new DataContractJsonSerializer(typeof(RegisterNodeRequest));
                Request = (RegisterNodeRequest)deserialized.ReadObject(ms);

            }
            return _SyncServer.RegisterNodeAsync(Request);
        }


        }
}
