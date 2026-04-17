using ISHMS.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.DAL.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<VitalSign> VitalSigns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>()
            .HasMany(p => p.VitalSigns)
            .WithOne(v => v.Patient)
            .HasForeignKey(v => v.PatientId);
    }
}