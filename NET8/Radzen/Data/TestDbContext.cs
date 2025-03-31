using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RadzenDemo.Models.TestDb;

namespace RadzenDemo.Data
{
    public partial class TestDbContext : DbContext
    {
        public TestDbContext()
        {
        }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RadzenDemo.Models.TestDb.MyGuest>()
              .Property(p => p.RegDate)
              .HasDefaultValueSql(@"CURRENT_TIMESTAMP");

            builder.Entity<RadzenDemo.Models.TestDb.UserAccount>()
              .Property(p => p.Name)
              .HasDefaultValueSql(@"'0'");
            this.OnModelBuilding(builder);
        }

        public DbSet<RadzenDemo.Models.TestDb.MyGuest> MyGuests { get; set; }

        public DbSet<RadzenDemo.Models.TestDb.UserAccount> UserAccounts { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new BlankTriggerAddingConvention());
        }
    }
}