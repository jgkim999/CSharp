using Demo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demo.Infra.Repositories;

public class DemoDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<CompanyEntity> Companies { get; set; }
    public DbSet<ProductEntity> Products { get; set; }
    public DbSet<OrderEntity> Orders { get; set; }
    
    public DemoDbContext(DbContextOptions<DemoDbContext> options)
        : base(options)
    {
    }
}
