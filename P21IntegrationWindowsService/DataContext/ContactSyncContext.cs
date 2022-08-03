using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Reflection;

namespace P21IntegrationWindowsService.Models
{
    public class ContactSyncContext : DbContext
    {
        private string DBPath
        {
            get
            {
                return "C:\\Integration\\contactsyncdb.db";
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($@"Data Source={DBPath}");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContactSync>().HasKey(c => new { c.id });
            modelBuilder.Entity<Settings>().HasKey(s => new { s.ICID });
            base.OnModelCreating(modelBuilder);
        }
       
        public DbSet<Settings> IntSystemSettings { get; set; }
        public DbSet<ContactSync> ContactSyncDB { get; set; }

    }
}
