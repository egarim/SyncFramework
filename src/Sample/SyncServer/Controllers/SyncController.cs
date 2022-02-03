using BIT.Data.Sync;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace BIT.Data.Sync.EfCore.Tests.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SyncController : ControllerBase
    {


        private readonly ILogger<SyncController> _logger;
        private readonly ISyncServer _SyncServer;
        protected string GetHeader(string HeaderName)
        {
            Microsoft.Extensions.Primitives.StringValues stringValues = HttpContext.Request.Headers[HeaderName];
            return stringValues;
        }


        public SyncController(ILogger<SyncController> logger, ISyncServer SyncServer)
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
            if (string.IsNullOrEmpty(body))
                return;
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(body)))
            {
              
                DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(List<Delta>));
                List<Delta> Deltas = (List<Delta>)deserializer.ReadObject(ms);
                await _SyncServer.SaveDeltasAsync(NodeId, Deltas, new CancellationToken());
                var Message = $"Push to node:{NodeId}{Environment.NewLine}Deltas Received:{Deltas.Count}{Environment.NewLine}Identity:{Deltas.FirstOrDefault()?.Identity}";
                _logger.LogInformation(Message);
                Debug.WriteLine(Message);
              
            }
        }
        [HttpGet("Fetch")]
        public async Task<string> Fetch(Guid startindex, string identity = null)
        {
            string NodeId = GetHeader("NodeId");
            var Message = $"Fetch from node:{NodeId}{Environment.NewLine}Start delta index:{startindex}{Environment.NewLine}Client identity:{identity}";
            _logger.LogInformation(Message);
            Debug.WriteLine(Message);
            IEnumerable<IDelta> enumerable;
            if (string.IsNullOrEmpty(identity))
                enumerable = await _SyncServer.GetDeltasAsync(NodeId, startindex, new CancellationToken());
            else
                enumerable = await _SyncServer.GetDeltasFromOtherNodes(NodeId, startindex, identity, new CancellationToken());
            List<Delta> toserialzie = new List<Delta>();
            var knowTypes = new List<Type>() { typeof(DateTimeOffset) };

            foreach (IDelta delta in enumerable)
            {
                toserialzie.Add(new Delta(delta));
            }
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(List<Delta>), knowTypes);
            MemoryStream msObj = new MemoryStream();
            js.WriteObject(msObj, toserialzie);
            msObj.Position = 0;
            StreamReader sr = new StreamReader(msObj);
            string jsonDeltas = sr.ReadToEnd();
            return jsonDeltas;

        }

    }
}
