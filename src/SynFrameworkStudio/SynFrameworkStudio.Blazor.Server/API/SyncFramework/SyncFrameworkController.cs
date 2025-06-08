using BIT.Data.Sync;
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
using System.Diagnostics;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Channels;
using System.Text;

[Route("api/[controller]")]
[ApiController]
//[Authorize]
public class SyncFrameworkController : SyncControllerBase
{
    IObjectSpaceFactory objectSpaceFactory;
    INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory;
    public SyncFrameworkController(ILogger<SyncControllerBase> logger, ISyncFrameworkServer syncServer, IObjectSpaceFactory objectSpaceFactory, INonSecuredObjectSpaceFactory nonSecuredObjectSpaceFactory) : base(logger, syncServer)
    {
        this.objectSpaceFactory = objectSpaceFactory;
        this.nonSecuredObjectSpaceFactory= nonSecuredObjectSpaceFactory;



    }

    public override Task<string> Fetch(string startIndex, string identity)
    {
        Task<string> Response = base.Fetch(startIndex, identity);
        var os = nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Events>();
        Events response = os.CreateObject<Events>();

        FetchOperationResponse FetchResponse = DeserializeFetchResponse(Response.Result);


        response.LoadFrom(FetchResponse);
        response.Date = DateOnly.FromDateTime(DateTime.Now);
        response.Time = TimeOnly.FromDateTime(DateTime.Now);
        os.CommitChanges();
        return Response;
    }

    [HttpGet]
    public IActionResult Get()
    {
        //using IObjectSpace newObjectSpace = objectSpaceFactory.CreateObjectSpace<Contact>();
        // ...
        return Ok();
    }
    public FetchOperationResponse DeserializeFetchResponse(string jsonString)
    {
        using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
        {
            DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(FetchOperationResponse));
            return (FetchOperationResponse)deserializer.ReadObject(ms);
        }
    }
    public PushOperationResponse DeserializeResponse(string jsonString)
    {
        using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString)))
        {
            DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(PushOperationResponse));
            return (PushOperationResponse)deserializer.ReadObject(ms);
        }
    }
    public override async Task<string> Push()
    {

        var resut = await base.Push();
        var PushOperationResult = DeserializeResponse(resut);
        var os= nonSecuredObjectSpaceFactory.CreateNonSecuredObjectSpace<Events>();

        Events  response= os.CreateObject<Events>();
        response.LoadFrom(PushOperationResult);
        response.Date = DateOnly.FromDateTime(DateTime.Now);
        response.Time = TimeOnly.FromDateTime(DateTime.Now);
        os.CommitChanges();


        return resut;
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