using BIT.Data.Sync.AspNetCore.Controllers;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SyncFramework.Dashboard.Controllers
{
    [ApiController]
    [Route("sync")]
    public class SyncController : SyncControllerBase
    {
        public SyncController(ILogger<SyncControllerBase> logger, ISyncFrameworkServer syncServer)
            : base(logger, syncServer) { }
    }
}
