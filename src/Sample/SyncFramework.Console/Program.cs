using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using BIT.Data.Sync.EfCore.Npgsql;
using System.Collections.Generic;
using BIT.Data.Sync.Client;
using SyncFramework.ConsoleApp.Models;
using SyncFramework.ConsoleApp.Context;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using BIT.Data.Sync.EfCore.SqlServer;
using Microsoft.Data.SqlClient;

namespace SyncFramework.ConsoleApp
{
    internal class Program
    {
        public static IConfiguration config;
        static IDictionary<string, TestPosContext> _contextList;
        static TestPosContext _context;
        static string _selectedClient;
        static HttpClient _server;
        static async Task Main(string[] args)
        {
            #region Configurations & Registration
            config = new ConfigurationBuilder()
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();
            //var httpFactory = new HttpClientFactory();
            IServiceCollection services = new ServiceCollection();
            IServiceCollection services2 = new ServiceCollection();
            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            Extensions.SyncFrameworkExtensions.AddEfSynchronization(services,
               /* Database connection for the delta */
               (options) =>
               {
                   // options.UseSqlServer(
                   //     config.GetConnectionString("DeltaStoreClient1")
                   //     )
                   //.LogTo(message => Debug.WriteLine(message));
                   options.UseNpgsql(config.GetConnectionString("DeltaStoreClient1"))
                    .LogTo(message => Debug.WriteLine(message));
                   /*Uncomment it for if you want to use Mssqlserver*/
                   //.LogTo(Console.WriteLine)
                   //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole())); 
               },
               "qamar.abbas" /* Server Node Id*/,
               config.GetSection("ServerUrl").Value /*Server Url*/,
               "Store1" /*Identity */,
               //new SqlServerDeltaGenerator()
               new NpgsqlDeltaGenerator() /*, new PomeloMySqlDeltaGenerator()*/)
               /*Uncomment it for if you want to use Mssqlserver*/
               //.AddEntityFrameworkSqlServer(); 
               .AddEntityFrameworkNpgsql();
            services.AddDbContext<TestPosContext>(options =>
            {
                /*Uncomment it for if you want to use Mssqlserver*/
                //options.UseSqlServer(config.GetConnectionString("ClientDB1"))
                //   .LogTo(message => Debug.WriteLine(message));
                options.UseNpgsql(config.GetConnectionString("ClientDB1"))
                .LogTo(message => Debug.WriteLine(message));
            });

            var serverVersion = new MySqlServerVersion(new Version(8, 0, 26));
            Extensions.SyncFrameworkExtensions.AddEfSynchronization(services2,
               /* Where to store the delta */
               (options) =>
               {
                   /*Uncomment it for if you want to use Mssqlserver*/
                   //options.UseSqlServer(config.GetConnectionString("DeltaStoreClient2"))
                   // .LogTo(message => Debug.WriteLine(message));
                   options.UseNpgsql(config.GetConnectionString("DeltaStoreClient1"))
                    .LogTo(message => Debug.WriteLine(message));
                   //.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole())); 
               },
              "qamar.abbas" /* Server Node Id*/,
              config.GetSection("ServerUrl").Value /*Server Url*/,
              "Store2" /*Identity */,
               //new SqlServerDeltaGenerator(),
               new NpgsqlDeltaGenerator())
              .AddEntityFrameworkNpgsql();
            /*Uncomment it for if you want to use Mssqlserver*/
            //.AddEntityFrameworkSqlServer();
            services2.AddDbContext<TestPosContext>(options =>
            {
                /*Uncomment it for if you want to use Mssqlserver*/
                //options.UseSqlServer(config.GetConnectionString("ClientDB2"))
                //    .LogTo(message => Debug.WriteLine(message));
                options.UseNpgsql(config.GetConnectionString("ClientDB2"))
                    .LogTo(message => Debug.WriteLine(message));
            });
            services.AddSingleton(f => services);
            var serviceProvider = services.BuildServiceProvider();
            var serviceProvider1 = services2.BuildServiceProvider();
            #endregion

            #region DbContext initialization
            using var MasterContext = serviceProvider.GetService<TestPosContext>();
            using var client2 = serviceProvider1.GetService<TestPosContext>();

            _server = new HttpClient()
            {
                BaseAddress = new Uri(config.GetSection("ServerUrl").Get<string>())
            };
            MasterContext.Database.EnsureDeleted();
            MasterContext.Database.EnsureCreated();

            client2.Database.EnsureDeleted();
            client2.Database.EnsureCreated();
            _contextList = new Dictionary<string, TestPosContext>();
            _contextList.Add("1", MasterContext);
            _contextList.Add("2", client2);
            _context = MasterContext; 
            #endregion
            _selectedClient = "1";
            ShowChoice();
            ConsoleKeyInfo key;
            bool autoClear = true;
            string client;
            do
            {
                try
                {
                    client = $"Client { _selectedClient}";
                    key = Console.ReadKey();
                    if (autoClear) Console.Clear();
                    Console.WriteLine();
                    ShowChoice();
                    switch (key.Key)
                    {
                        case ConsoleKey.D1:
                            Console.Clear();
                            SelectClient();
                            break;
                        case ConsoleKey.D2:
                            await CreateOrder();                            
                            ShowOrders();
                            break;
                        case ConsoleKey.D3:
                            ShowOrders();
                            break;
                        case ConsoleKey.D4:
                            await EditOrder();
                            break;
                        case ConsoleKey.D5:
                            DeleteOrder();
                            break;                      
                        case ConsoleKey.D6:
                            await _context.PushAsync();
                            System.Console.WriteLine($"\n{client}: Pushed to Server");
                            break;
                        case ConsoleKey.D7:
                            await _context.PullAsync();
                            System.Console.WriteLine($"\n{client}: Orders Pulled from Server");
                            ShowOrders();
                            break;
                        case ConsoleKey.D8:
                            await ShowDeltats();
                            break;
                        case ConsoleKey.D9:
                            await _context.DeltaStore.PurgeDeltasAsync(_context.Identity, System.Threading.CancellationToken.None);
                            await ShowDeltats();
                            break;
                        case ConsoleKey.D0:
                            await _context.DeltaStore.ResetDeltasStatusAsync(_context.Identity);
                            Console.WriteLine("\nDeltaSync status reseted successfully.");
                            break;
                        case ConsoleKey.OemMinus:
                            await ShowServerDeltas();
                            break;
                        case ConsoleKey.F2:
                            Console.Clear();
                            ShowChoice();
                            break;
                        case ConsoleKey.F3:
                            autoClear = !autoClear;
                            break;
                        case ConsoleKey.F4:
                            break;
                    }                    
                }
                catch (Exception ex)
                {
                    Console.Clear();
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine("Press any key to continue...");
                    key = Console.ReadKey();
                }
            } while (key.Key != ConsoleKey.Escape && key.Key != ConsoleKey.Q);            await MasterContext.PushAsync();           
        }
        static void DeleteOrder()
        {
            Console.Clear();
            ShowOrders();
            Console.Write("\nPlease enter Order Id to delete: ");
            var guid = Console.ReadLine();
            Guid id;
            if(Guid.TryParse(guid, out id))
            {
                _context.Orders.Remove(_context.Orders.FirstOrDefault(f => f.Id == id));
                _context.SaveChanges();
                Console.WriteLine("Record Delted successfully");
            }
            else
            {
                Console.WriteLine("Invalid order id entered");
            }
        }
        static void SelectClient()
        {
            foreach (var item in _contextList.Keys)
            {
                System.Console.WriteLine($"Press {item}: Client {item}");
            }
            do
            {
                Console.Write("Please select a client: ");
                var key = Console.ReadKey();
                if (_contextList.ContainsKey(key.KeyChar.ToString()))
                {
                    _context = _contextList[key.KeyChar.ToString()];
                    _selectedClient = key.KeyChar.ToString();
                    Console.Clear();
                    break;
                }
                else
                    Console.WriteLine("No client exists");
            }
            while (true);
            ShowChoice();
        }
        static void ShowChoice()
        {
            System.Console.WriteLine("==============================");
            Console.WriteLine($"**SELECTED [CLIENT {_selectedClient}]**");
            System.Console.WriteLine("==============================");

            string client1 = $"[Client {_selectedClient}]";
            Console.WriteLine("Please Select an option...");
            Console.WriteLine();
            System.Console.WriteLine($"Press (1): Change/Select Client");
            Console.WriteLine();
            System.Console.WriteLine($"Press (2): Create an Order");            
            System.Console.WriteLine($"Press (3): View order");
            System.Console.WriteLine($"Press (4): Edit order");
            Console.WriteLine($"Press (5): Delete Order");
            Console.WriteLine();
            System.Console.WriteLine($"Press (6): Push Orders");
            System.Console.WriteLine($"Press (7): Pull data from Server");
            Console.WriteLine();
            System.Console.WriteLine($"Press (8): View Deltas");
            System.Console.WriteLine($"Press (9): Purge Delta Store");
            System.Console.WriteLine($"Press (0): Reset DeltaStatus");
            System.Console.WriteLine($"Press (-): View Server Deltas");
            Console.WriteLine();
            System.Console.WriteLine("Press (F2): Clear Screen");
            System.Console.WriteLine($"Press (F3): Toggle Auto Clear");
            System.Console.WriteLine($"Press Escape or q to quit");
            System.Console.WriteLine("...");
            System.Console.Write("Select a choice: ");

        }       

        static void ShowOrders()
        {
            var orders = _context.Orders;
            Console.WriteLine();
            System.Console.WriteLine("\n----------------------------------------------------------------------");
            Console.WriteLine($"Client {_selectedClient} Orders: Total Found {orders.Count()}");
            System.Console.WriteLine("----------------------------------------------------------------------");
            foreach (var o in orders)
            {
                Console.WriteLine($"{o.Id}\t{o.OrderDate.DateTime}\t {o.Description}");
            }           
        }
        static async Task ShowDeltats()
        {
            var deltas = await _context.DeltaStore.GetDeltasByIdentityAsync(Guid.Empty, _context.Identity, CancellationToken.None).ConfigureAwait(false);
            System.Console.WriteLine("\n----------------------------------------------------------------------");
            Console.WriteLine($"Client {_selectedClient} Deltas: Total Found {deltas.Count()}");
            System.Console.WriteLine("----------------------------------------------------------------------");
            foreach (var i in deltas)
            {
                Console.WriteLine($"{i.Date}\t{i.Index}\t{i.Epoch}\t{i.Identity}");
            }
        }
        static async Task CreateOrder()
        {
            System.Console.WriteLine("Please Enter Order Details");
            var str = System.Console.ReadLine();
            var o= new Order
            {
                Id = Guid.NewGuid(),
                Description = str,
                OrderDate = DateTime.Now,
            };
            _context.Orders.Add(o);
            await _context.SaveChangesAsync();
            Console.WriteLine($"Client {_selectedClient}: Order Saved Successfully");
        }
        static async Task ShowServerDeltas()
        {
            var deltas = await _context.FetchAsync();
            Console.WriteLine("\n----------------------------------------------------------------------");
            Console.WriteLine($"Server Deltas: Total Found {deltas.Count()}");
            System.Console.WriteLine("----------------------------------------------------------------------");
            foreach (var i in deltas)
            {
                Console.WriteLine($"{i.Date}\t{i.Index}\t{i.Epoch}\t{i.Identity}");
            }
        }
        static async Task EditOrder()
        {
            ShowOrders();
            Console.Write("\nEnter Edit Order Id: ");
            var input = Console.ReadLine();
            Guid id;
            if (!Guid.TryParse(input, out id))
            {
                Console.WriteLine("Invalid order id entered.");
                return;
            }
            Console.Write("\nEnter Order Description:");
            input = Console.ReadLine();
            var order = await _context.Orders.FirstOrDefaultAsync(f => f.Id == id);
            if (order == null)
            {
                Console.WriteLine("Order not found");
                return;
            }
            order.OrderDate = DateTime.Now;
            order.Description = input;
            _context.SaveChanges();
            Console.WriteLine("Record saved successfully");
        }
    }
}
