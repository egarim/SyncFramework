using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncFramework.ConsoleApp.Helper;
using SyncFramework.ConsoleApp.Models;
using SyncFramework.ConsoleApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFramework.ConsoleApp.Context
{
    public class TestPosContext2: DbContext
    {
        //protected TestPosContext()
        //{

        //}
        public TestPosContext2(DbContextOptions<TestPosContext2> options, 
            IServiceProvider serviceProvider) :
            base(options /*options*/)
        {
            //ServiceCollection.AddSingleton<ISyncIdentityService>(new SyncIdentityService(Identity));
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}
