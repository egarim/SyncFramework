using BIT.Data.Sync;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.EfCore.Sync.Test.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SyncController : ControllerBase
    {


        private readonly ILogger<SyncController> _logger;
        private readonly ISyncServerNode _SyncServer;
        protected string GetHeader(string HeaderName)
        {
            return this.HttpContext.Request.Headers[HeaderName];
        }


        public SyncController(ILogger<SyncController> logger, ISyncServerNode SyncServer)
        {
            _logger = logger;
            _SyncServer = SyncServer;
        }
        [HttpPost(nameof(Push))]
        public virtual async Task Push([FromBody] List<Delta> deltas)
        {

            string DeltaStoreName = GetHeader("DeltaStoreName");
            string DeltaProcessorName = GetHeader("DeltaProcessorName");
            await this._SyncServer.SaveDeltasAsync(DeltaStoreName, deltas, new CancellationToken());
            //await this._SyncServer.ProcessDeltasAsync(DeltaProcessorName, deltas);
        }
        [HttpGet("Fetch")]
        public async Task<IEnumerable<IDelta>> Fetch(Guid startindex, string identity)
        {
            string name = GetHeader("DeltaStoreName");
            IEnumerable<IDelta> enumerable = await this._SyncServer.GetDeltasAsync(name, startindex, identity,new CancellationToken());
            return enumerable;

        }

    }
}
