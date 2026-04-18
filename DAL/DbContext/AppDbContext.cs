using ISHMS.Core.Models;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.DAL.DbContext;

public class AppDbContext : IdentityDbContext<ApplicationUser>
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
        base.OnModelCreating(modelBuilder);
        
        SeedRoles(modelBuilder);
    }
    private void SeedRoles(ModelBuilder builder)
    {
        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "1",
                Name = Core.Constants.AppRoles.Admin,
                NormalizedName = Core.Constants.AppRoles.Admin.ToUpper()
                
            },
            new IdentityRole
            {
                Id = "2",
                Name = Core.Constants.AppRoles.Doctor,
                NormalizedName = Core.Constants.AppRoles.Doctor.ToUpper()
            },
            new IdentityRole
            {
                Id = "3",
                Name = Core.Constants.AppRoles.Nurse,
                NormalizedName = Core.Constants.AppRoles.Nurse.ToUpper()
            },
            new IdentityRole
            {
                Id = "4",
                Name = Core.Constants.AppRoles.Receptionist,
                NormalizedName = Core.Constants.AppRoles.Receptionist.ToUpper()
            }
        );
    }
}