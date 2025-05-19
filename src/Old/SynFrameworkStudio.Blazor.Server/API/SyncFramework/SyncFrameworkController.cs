using BIT.Data.Sync.AspNetCore.Controllers;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.WebApi.Xpo;
using DevExpress.ExpressApp.Xpo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SynFrameworkStudio.Blazor.Server;
using SynFrameworkStudio.Module.BusinessObjects.Sync;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SyncFrameworkController : SyncControllerBase
{
    IObjectSpaceFactory objectSpaceFactory;
    public SyncFrameworkController(ILogger<SyncControllerBase> logger, ISyncServer syncServer, IObjectSpaceFactory objectSpaceFactory) : base(logger, syncServer)
    {
        this.objectSpaceFactory = objectSpaceFactory;



     
        


        syncServer.RegisterNodeFunction = (request) =>
        {
            string nodeId = request.Options.FirstOrDefault(k => k.Key == "NodeId").Value.ToString();
            string ConnectionString = request.Options.FirstOrDefault(k => k.Key == "ConnectionString").Value.ToString();
            XpoTypesInfoHelper.GetXpoTypeInfoSource();
            XafTypesInfo.Instance.RegisterEntity(typeof(XpoDelta));
            XPObjectSpaceProvider osProvider = new XPObjectSpaceProvider(
            ConnectionString, null);

            osProvider.SchemaUpdateMode = SchemaUpdateMode.DatabaseAndSchema;
            var UpdateOs=osProvider.CreateUpdatingObjectSpace(true) as XPObjectSpace;

            UpdateOs.Session.UpdateSchema(false);


            XpoSequenceService xpoSequenceService = new XpoSequenceService(osProvider);
            XpoDeltaStore xpoDeltaStore = new XpoDeltaStore(xpoSequenceService, osProvider);
           
            return new SyncServerNode(xpoDeltaStore, null, nodeId);
        };
    }
    [HttpGet]
    public IActionResult Get()
    {
        //using IObjectSpace newObjectSpace = objectSpaceFactory.CreateObjectSpace<Contact>();
        // ...
        return Ok();
    }
    //...
}