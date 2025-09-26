using Microsoft.EntityFrameworkCore;
using SqlToXmlConverter.Models;

namespace SqlToXmlConverter.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Employee entity
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            entity.Property(e => e.Department)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.HireDate)
                .IsRequired();
            entity.Property(e => e.Salary)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
        });
    }
}
