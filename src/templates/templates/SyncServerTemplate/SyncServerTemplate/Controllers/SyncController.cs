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
using BIT.Data.Sync.AspNetCore.Controllers;

namespace SyncServerTemplate.Controllers
{
    
    [ApiController]
    [Route("[controller]")]
    public class SyncController : SyncControllerBase
    {
        public SyncController(ILogger<SyncControllerBase> logger, ISyncServer SyncServer) : base(logger, SyncServer)
        {
        }
        public override Task<string> Fetch(string startIndex, string identity)
        {
            return base.Fetch(startIndex, identity);
        }
        public override Task Push()
        {
            return base.Push();
        }       
    }
}
