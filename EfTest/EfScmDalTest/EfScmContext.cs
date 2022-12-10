using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfScmDataAccess;
using Microsoft.EntityFrameworkCore;

namespace EfScmDalTest
{
    public class EfScmContext : DbContext
    {

        public DbSet<PartType> Parts { get; set; }
        public DbSet<InventoryItem> Inventory { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=efscm.db");
        }
    }
}