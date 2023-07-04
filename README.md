# SyncFramework
is c# a library that helps you synchronize any type of data, using [delta encoding technology](https://en.wikipedia.org/wiki/Delta_encoding) 

There are currently 2 implementations of the SyncFramework


- SyncFramework for XPO
- SyncFramework for Entity Framework Core

## in a nutshell

Data synchronization in EFCore is accomplished by replacing the DbContext's internal service collection with a custom one, containing the SyncFramework services.

The SyncFramework services are registered in the service collection using the extension method, AddEfSynchronization.

SyncFramework replaces the BatchExecutor service with a custom one, capable of intercepting the generation of SQL commands (Deltas), and storing them so they can be executed in a remote database.

Deltas are typically generated for the same database engine in use, but they can also be generated for a different database. For instance, your main database could be MS SQL Server, but your remote database could be PostgreSQL.

If you want to learn more about data synchronization you can checkout the following
blog posts:

1. Data synchronization in a few words - https://www.jocheojeda.com/2021/10/10/data-synchronization-in-a-few-words/
2. Parts of a Synchronization Framework - https://www.jocheojeda.com/2021/10/10/parts-of-a-synchronization-framework/
3. Let’s write a Synchronization Framework in C# - https://www.jocheojeda.com/2021/10/11/lets-write-a-synchronization-framework-in-c/
4. Synchronization Framework Base Classes - https://www.jocheojeda.com/2021/10/12/synchronization-framework-base-classes/
5. Planning the first implementation - https://www.jocheojeda.com/2021/10/12/planning-the-first-implementation/
6. Testing the first implementation - https://youtu.be/l2-yPlExSrg 
7. Adding network support - https://www.jocheojeda.com/2021/10/17/syncframework-adding-network-support/


## Current version 7.0.X

Target Framework net6.0

- EfCore Version        7.0.3
- Postgres Version      7.0.3
- Pomelo Mysql Version  7.0.0
- Sqlite Version		7.0.3
- Sql Server Version    7.0.3

## Nugets

- Common base classes and interfaces to implement delta synchronization https://www.nuget.org/packages/BIT.Data.Sync/
- EfCore Implementation https://www.nuget.org/packages/BIT.Data.Sync.EfCore/
- EfCore Implementation for postgres https://www.nuget.org/packages/BIT.Data.Sync.EfCore.Npgsql/
- EfCore Implementation for Mysql/MariaDB using Pomelo provider https://www.nuget.org/packages/BIT.Data.Sync.EfCore.Pomelo.MySql/
- EfCore Implementation for Sqlite https://www.nuget.org/packages/BIT.Data.Sync.EfCore.Sqlite/
- EfCore Implementation for MsSqlServer https://www.nuget.org/packages/BIT.Data.Sync.EfCore.SqlServer/



## Getting started

The first step is to install the Nuget package that matches your current database, for example if you are using
MsSqlServer you should install the following Nuget package

BIT.Data.Sync.EfCore.SqlServer

If you want to use the SyncFramework with a different database engine, you should install the corresponding Nuget package.

Let's prepare the services needed for database synchronization

### Service collection
```csharp

//The options needed for the DbContext, you can build them in any way you want, in this case we are using Sqlite
DbContextOptionsBuilder OptionsBuilder = new DbContextOptionsBuilder();
OptionsBuilder.UseSqlite(ConnectionString);

//The http client is used to send the deltas to the server
 HttpClient Client = new HttpClient();
 Client.BaseAddress = new Uri("https://ReplaceWithYourServerUrl/");

 //The DeltaGenerator is used to generate the deltas, you can add as many as you want, in this case we are using the Sqlite and SqlServer generators
 List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
 DeltaGenerators.Add(new SqliteDeltaGenerator());
 DeltaGenerators.Add(new SqlServerDeltaGenerator());
 DeltaGeneratorBase[] additionalDeltaGenerators = DeltaGenerators.ToArray();

//We prepare the service collection that will be used to register the SyncFramework services
ServiceCollection ServiceCollection = new ServiceCollection();
ServiceCollection.AddEfSynchronization((options) =>
{
    string ConnectionString $"Data Source=Deltas.db;";
    //The EfSynchronizationOptions is used to configure the database synchronization
    options.UseSqlite(ConnectionString);// we are going to store the deltas in a Sqlite database
    //it's possible to store deltas any any of the supported database engines, for example you could use MsSqlServer
    //options.UseSqlServer(ConnectionString);
},
//The http client that will send the data to the server
Client, 
//the ID of the delta store on the server side,a sync server can have multiple delta stores
"MemoryDeltaStore1",
//the ID of this client, each client has to have an unique id
Maui",
//Additional delta generators, in this case we are using the Sqlite and SqlServer generators
additionalDeltaGenerators);

//We add the entity framework services, in this case we are using Sqlite
ServiceCollection.AddEntityFrameworkSqlite();

//We build the service provider
var ServiceProvider = ServiceCollection.BuildServiceProvider();
```

### The DbContext
You should use SyncFrameworkDbContext instead of the regular DbContext. SyncFrameworkDbContext is a subclass of the regular DbContext. The only difference is that it implements all the boilerplate code needed for synchronization, so you can use it in the same way.

```csharp

MyDbContext MyDbContext = new MyDbContext(OptionsBuilder.Options, ServiceProvider);
MyDbContext.Database.EnsureCreated();

```

If you don't want to use SyncFrameworkDbContext you can extend your own DbContext by implementing ISyncClientNode interface.

```csharp

public class MyAppDbContext : DbContext, ISyncClientNode

```

Then, you need to create a new instance of the delta processor and the services provider that we built in previous steps. Let's do that in the constructor of your DbContext.

```csharp

public MyAppDbContext(DbContextOptions options,IServiceProvider serviceProvider) : base(options)
{
    this.serviceProvider = serviceProvider;
    this.DeltaProcessor = new EFDeltaProcessor(this);
} 

```

Then we need to replaces the service provider with our own, let's do that by override or extend the configuration method as follows

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    this.Identity = serviceProvider.GetService<ISyncIdentityService>()?.Identity;
    this.DeltaStore = serviceProvider.GetService<IDeltaStore>();
    this.SyncFrameworkClient = serviceProvider.GetService<ISyncFrameworkClient>();
    optionsBuilder.UseInternalServiceProvider(serviceProvider);
}
```
Our DbContext is able to store and process deltas and push and pull deltas from a SyncServer.

We are done!!! 


### Fetching pulling and pushing deltas

The operations of fetch, pull and push are generic and they are implemented as extension methods of in the ISyncClientNode interface.

- Fetch:Fetches the deltas from the server and returns them as a list of Delta objects
- Pull: Fetches the deltas from the server, process them by execute the sql operations in your local database
- Push: Pushes the deltas to the server

```csharp

await MyDbContext.PushAsync();

await MyDbContext.PullAsync();

await MyDbContext.FetchAsync();

 ```

### Best practices using your DbConext to push and pull deltas

You should use your DbContext withing a using statement, this will ensure that the DbContext is disposed and the connection is closed, also that the http client is not dissposed (this can happend in platforms like MAUI).

```csharp
using (var context = GetDbContext())
{
  // use your conext here
}
```
### Implementing a SyncServer

The easiest way to implement a server is to add register a instace of SyncServer in the service collection.
```csharp

public void ConfigureServices(IServiceCollection services)
{
    SyncServerNode syncServerNode = new SyncServerNode(new MemoryDeltaStore(), null, "MemoryDeltaStore1");
    services.AddSingleton<ISyncServer>(new BIT.Data.Sync.Server.SyncServer(syncServerNode));
}

```

and use that service in your controller

```csharp

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
```
### Installing project templates

```

dotnet new -i BIT.Data.Sync.Templates

```
### To create a new SyncServer

```

dotnet new SyncServer -o MySyncServer

```
## Changelog

You can see the current list of changes [here](Changes.md)
