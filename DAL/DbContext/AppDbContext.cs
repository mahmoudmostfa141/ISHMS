using ISHMS.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<VitalSign> VitalSigns { get; set; }

    public DbSet<Department> Departments { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Bed> Beds { get; set; }

    //public DbSet<WaitingPatient> WaitingPatients { get; set; }

    public DbSet<PatientTask> PatientTasks { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<MedicalReport> MedicalReports { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // PatientTask → AssignedToUser (nullable, no cascade)
        builder.Entity<PatientTask>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Alert → TargetUser (nullable, no cascade)
        builder.Entity<Alert>()
            .HasOne(a => a.TargetUser)
            .WithMany()
            .HasForeignKey(a => a.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // MedicalReport → Doctor (restrict, مينفعش تحذف Doctor وعنده Reports)
        builder.Entity<MedicalReport>()
            .HasOne(r => r.Doctor)
            .WithMany()
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
