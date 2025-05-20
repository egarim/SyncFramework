using BIT.Data.Sync.AspNetCore.Controllers;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.WebApi.Xpo;
using DevExpress.ExpressApp.Xpo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynFrameworkStudio.Module.BusinessObjects.Sync;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
public class SyncFrameworkController : SyncControllerBase
{
    IObjectSpaceFactory objectSpaceFactory;
    public SyncFrameworkController(ILogger<SyncControllerBase> logger, ISyncServer syncServer, IObjectSpaceFactory objectSpaceFactory) : base(logger, syncServer)
    {
        this.objectSpaceFactory = objectSpaceFactory;

      
    }

    public override Task<string> Fetch(string startIndex, string identity)
    {
        return base.Fetch(startIndex, identity);
    }

    [HttpGet]
    public IActionResult Get()
    {
        //using IObjectSpace newObjectSpace = objectSpaceFactory.CreateObjectSpace<Contact>();
        // ...
        return Ok();
    }

    public override Task<string> Push()
    {
        return base.Push();
    }

    public override Task<IActionResult> RegisterNode()
    {
        return base.RegisterNode();
    }

    protected override string CreateFetchResponse(FetchOperationResponse fetchOperationResponse)
    {
        return base.CreateFetchResponse(fetchOperationResponse);
    }

    protected override void SetException(FetchOperationResponse fetchOperationResponse, Exception argEx, string anargumentnullexceptionoccurred)
    {
        base.SetException(fetchOperationResponse, argEx, anargumentnullexceptionoccurred);
    }
   
}