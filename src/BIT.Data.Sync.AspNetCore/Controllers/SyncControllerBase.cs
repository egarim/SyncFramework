
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
        public virtual async Task Push()
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

            }




        }
        [HttpGet("Fetch")]
        public virtual async Task<string> Fetch(string startIndex, string identity)
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

    }
}
