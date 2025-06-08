using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
using DevExpress.Data.Linq;
using DevExpress.Data.Linq.Helpers;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.ApplicationBuilder.Internal;
using DevExpress.ExpressApp.Blazor.ApplicationBuilder;
using DevExpress.ExpressApp.Blazor.Services;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.Authentication;
using DevExpress.ExpressApp.Security.Authentication.ClientServer;
using DevExpress.ExpressApp.WebApi.Services;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SynFrameworkStudio.Blazor.Server.Services;
using SynFrameworkStudio.Module;
using SynFrameworkStudio.Module.BusinessObjects.Sync;
using SynFrameworkStudio.WebApi.JWT;
using System.ComponentModel;
using System.Text;

namespace SynFrameworkStudio.Blazor.Server;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
        services.AddSingleton(typeof(Microsoft.AspNetCore.SignalR.HubConnectionHandler<>), typeof(ProxyHubConnectionHandler<>));

        services.AddCors(options => {
            options.AddPolicy("AllowBlazorApp",
                builder => {
                    builder.WithOrigins("https://localhost:7029") // Replace with your Blazor app's URL
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
        });

        services.AddSyncServer((request) =>
        {


            string nodeId = request.Options.FirstOrDefault(k => k.Key == "NodeId").Value.ToString();
            string ConnectionString = request.Options.FirstOrDefault(k => k.Key == "ConnectionString").Value.ToString();

            IDeltaStore EfDeltaStore = CreateEfDeltaStore(ConnectionString);

            return new SyncServerNode(EfDeltaStore, null, nodeId);



        });

        services.AddScoped<SelectedServerNode>();
        services.AddRazorPages();
        services.AddServerSideBlazor();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthenticationTokenProvider, JwtTokenProviderService>();
        services.AddScoped<CircuitHandler, CircuitHandlerProxy>();
        services.AddXaf(Configuration, builder => {
            builder.UseApplication<SynFrameworkStudioBlazorApplication>();



            builder.ObjectSpaceProviders.Events.OnObjectSpaceCreated = context => {
                var nonPersistentObjectSpace = context.ObjectSpace as NonPersistentObjectSpace;
                if (nonPersistentObjectSpace != null)
                {
                    nonPersistentObjectSpace.ObjectsGetting += NonPersistentObjectSpace_ObjectsGetting;
                }
            };

            builder.AddXafWebApi(webApiBuilder => {
                webApiBuilder.AddXpoServices();

                webApiBuilder.ConfigureOptions(options => {
                    // Make your business objects available in the Web API and generate the GET, POST, PUT, and DELETE HTTP methods for it.
                    // options.BusinessObject<YourBusinessObject>();
                });
            });

            builder.Modules
                .AddCloning()
                .AddConditionalAppearance()
                .AddDashboards(options => {
                    options.DashboardDataType = typeof(DevExpress.Persistent.BaseImpl.DashboardData);
                })
                .AddFileAttachments()
                .AddReports(options => {
                    options.EnableInplaceReports = true;
                    options.ReportDataType = typeof(DevExpress.Persistent.BaseImpl.ReportDataV2);
                    options.ReportStoreMode = DevExpress.ExpressApp.ReportsV2.ReportStoreModes.XML;
                })
                .AddValidation(options => {
                    options.AllowValidationDetailsAccess = false;
                })
                .Add<SynFrameworkStudio.Module.SynFrameworkStudioModule>()
                .Add<SynFrameworkStudioBlazorModule>();


          
            builder.ObjectSpaceProviders
                .AddSecuredXpo((serviceProvider, options) => {
                    string connectionString = null;
                    if(Configuration.GetConnectionString("ConnectionString") != null) {
                        connectionString = Configuration.GetConnectionString("ConnectionString");
                    }
#if EASYTEST
                    if(Configuration.GetConnectionString("EasyTestConnectionString") != null) {
                        connectionString = Configuration.GetConnectionString("EasyTestConnectionString");
                    }
#endif
                    ArgumentNullException.ThrowIfNull(connectionString);
                    options.ConnectionString = connectionString;
                    options.ThreadSafe = true;
                    options.UseSharedDataStoreProvider = true;
                    
                })
                .AddNonPersistent();
            builder.Security
                .UseIntegratedMode(options => {
                    options.Lockout.Enabled = true;

                    options.RoleType = typeof(PermissionPolicyRole);
                    // ApplicationUser descends from PermissionPolicyUser and supports the OAuth authentication. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/402197
                    // If your application uses PermissionPolicyUser or a custom user type, set the UserType property as follows:
                    options.UserType = typeof(SynFrameworkStudio.Module.BusinessObjects.ApplicationUser);
                    // ApplicationUserLoginInfo is only necessary for applications that use the ApplicationUser user type.
                    // If you use PermissionPolicyUser or a custom user type, comment out the following line:
                    options.UserLoginInfoType = typeof(SynFrameworkStudio.Module.BusinessObjects.ApplicationUserLoginInfo);
                    options.UseXpoPermissionsCaching();
                    options.Events.OnSecurityStrategyCreated += securityStrategy => {
                        // Use the 'PermissionsReloadMode.NoCache' option to load the most recent permissions from the database once
                        // for every Session instance when secured data is accessed through this instance for the first time.
                        // Use the 'PermissionsReloadMode.CacheOnFirstAccess' option to reduce the number of database queries.
                        // In this case, permission requests are loaded and cached when secured data is accessed for the first time
                        // and used until the current user logs out.
                        // See the following article for more details: https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Security.SecurityStrategy.PermissionsReloadMode.
                        ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.NoCache;
                    };
                })
                .AddPasswordAuthentication(options => {
                    options.IsSupportChangePassword = true;
                });
        });
        var authentication = services.AddAuthentication(options => {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        });
        authentication.AddCookie(options => {
            options.LoginPath = "/LoginPage";
        });
        authentication.AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters() {
                ValidateIssuerSigningKey = true,
                //ValidIssuer = Configuration["Authentication:Jwt:Issuer"],
                //ValidAudience = Configuration["Authentication:Jwt:Audience"],
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:Jwt:IssuerSigningKey"]))
            };
        });
        services.AddAuthorization(options => {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .RequireXafAuthentication()
                    .Build();
        });

        services
            .AddControllers()
            .AddOData((options, serviceProvider) => {
                options
                    .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel())
                    .EnableQueryFeatures(100);
            });

        services.AddSwaggerGen(c => {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo {
                Title = "SynFrameworkStudio API",
                Version = "v1",
                Description = @"Use AddXafWebApi(options) in the SynFrameworkStudio.Blazor.Server\Startup.cs file to make Business Objects available in the Web API."
            });
            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme() {
                Type = SecuritySchemeType.Http,
                Name = "Bearer",
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                {
                    new OpenApiSecurityScheme() {
                        Reference = new OpenApiReference() {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "JWT"
                        }
                    },
                    new string[0]
                },
            });
        });

        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => {
            //The code below specifies that the naming of properties in an object serialized to JSON must always exactly match
            //the property names within the corresponding CLR type so that the property names are displayed correctly in the Swagger UI.
            //XPO is case-sensitive and requires this setting so that the example request data displayed by Swagger is always valid.
            //Comment this code out to revert to the default behavior.
            //See the following article for more information: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializeroptions.propertynamingpolicy
            o.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
    }

    private static IDeltaStore CreateEfDeltaStore(string ConnectionString)
    {
        var options = new DbContextOptionsBuilder<DeltaDbContext>()
                  .UseSqlServer(ConnectionString)
                  .Options;

        DeltaDbContext deltaDbContext = new(options);


        YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
        EfSequenceService sequenceService = new EfSequenceService(implementationInstance, deltaDbContext);

        IDeltaStore EfDeltaStore = new EfDeltaStore(deltaDbContext, sequenceService);
        return EfDeltaStore;
    }

    private void NonPersistentObjectSpace_ObjectsGetting(object sender, ObjectsGettingEventArgs e)
    {

        var objectSpace = sender as NonPersistentObjectSpace;
        var Server = objectSpace.ServiceProvider.GetService(typeof(ISyncFrameworkServer)) as ISyncFrameworkServer;

        var selectedServerNode = objectSpace.ServiceProvider.GetService(typeof(SelectedServerNode)) as SelectedServerNode;

        SyncServerNode Node = Server.Nodes.FirstOrDefault(n=>n.NodeId == selectedServerNode.Node) as SyncServerNode;

        if(Node == null)
            return;

        var Ef = Node.DeltaStore as EfDeltaStore;




        var currentDeltas = Ef.DeltaDbContext.Deltas;


        CriteriaToExpressionConverter converter = new CriteriaToExpressionConverter();
        IQueryable<IDelta> filteredData = currentDeltas.AppendWhere(converter, e.Criteria) as IQueryable<IDelta>;

        if (e.ObjectType == typeof(DeltaRecord))
        {

            BindingList<DeltaRecord> objects = new BindingList<DeltaRecord>();

            foreach (var item in filteredData)
            {
                objects.Add(new DeltaRecord(item));
            }
            e.Objects = objects;
        }
    }

    //private void NonPersistentObjectSpace_ObjectsGetting(object sender, ObjectsGettingEventArgs e)
    //{

    //    var objectSpace = sender as NonPersistentObjectSpace;
    //    var Server=  objectSpace.ServiceProvider.GetService(typeof(ISyncServer)) as ISyncServer;
    //    SyncServerNode Node = Server.Nodes.FirstOrDefault() as SyncServerNode;
    //    var Ef=   Node.DeltaStore as EfDeltaStore;

    //    var currentDeltas = Ef.DeltaDbContext.Deltas;




    //    if (e.ObjectType == typeof(DeltaRecord))
    //    {

    //        BindingList<DeltaRecord> objects = new BindingList<DeltaRecord>();

    //        foreach (var item in currentDeltas)
    //        {
    //            objects.Add(new DeltaRecord(item));
    //        }
    //        e.Objects = objects;
    //    }
    //}

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if(env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SynFrameworkStudio WebApi v1");
            });
        }
        else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseCors("AllowBlazorApp");

        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseXaf();

       var syncServer=  app.ApplicationServices.GetService<ISyncFrameworkServer>();
        var config = app.ApplicationServices.GetService<IConfiguration>();


        var cnx= config.GetValue<string>("ConnectionStrings:ConnectionString");
        //XpoTypesInfoHelper.GetXpoTypeInfoSource();

        XPObjectSpaceProvider osProvider = new XPObjectSpaceProvider(
        cnx, null);
        IObjectSpace os = osProvider.CreateObjectSpace();
        XpoTypesInfoHelper.GetXpoTypeInfoSource();
        XafTypesInfo.Instance.RegisterEntity(typeof(ServerNode));

        os.GetObjectsQuery<ServerNode>().Where(n=>n.Active).ToList().ForEach(n =>
        {
            syncServer.RegisterNodeAsync(new SyncServerNode(CreateEfDeltaStore(n.ConnectionString), null, n.NodeId));
        });



        app.UseEndpoints(endpoints => {
            endpoints.MapXafEndpoints();
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/_Host");
            endpoints.MapControllers();
        });
    }
}
