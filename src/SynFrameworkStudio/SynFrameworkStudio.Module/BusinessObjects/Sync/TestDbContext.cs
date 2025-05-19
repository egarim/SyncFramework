using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
    public class Data
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key, Column(Order = 0)]
        public Guid Id { get; set; }

        public string Text { get; set; }


     
    }
    public class TestDbContext: SyncFrameworkDbContext
    {
        public TestDbContext(DbContextOptions options, IServiceProvider SyncFrameworkServiceCollection) : base(options, SyncFrameworkServiceCollection)
        {
           
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseInMemoryDatabase("TestDbContext");
            base.OnConfiguring(optionsBuilder);
        }
        public DbSet<Data> DataRecords { get; set; }
    }
}
