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
using BIT.Data.Sync.AspNetCore.Controllers;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Tests.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class SyncController : SyncControllerBase
    {
        public SyncController(ILogger<SyncControllerBase> logger, ISyncServer SyncServer) : base(logger, SyncServer)
        {
            var ServerWithEvents= SyncServer as ISyncServerWithEvents;
            if (ServerWithEvents != null)
            {
                ServerWithEvents.SavingDelta += ServerWithEvents_SavingDelta; ;
            }
        }

        private void ServerWithEvents_SavingDelta(object sender, ServerSavingDeltaEventArgs e)
        {
           Debug.WriteLine($"Saving Delta {e.Delta.Index}");
        }

        public override Task<string> Fetch(string startIndex, string identity)
        {
            return base.Fetch(startIndex, identity);
        }

        public override Task<string> Push()
        {
            return base.Push();
        }
        public override Task<bool> RegisterNode()
        {
            return base.RegisterNode();
        }
    }
}
