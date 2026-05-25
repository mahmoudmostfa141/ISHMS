using ISHMS.Core.Constants.Enums;
using ISHMS.Core.Enums;
using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.API.Seeding
{
    public class TestPatientSeeder
    {
        private readonly AppDbContext _context;

        public TestPatientSeeder(AppDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            var patientsToAdd = new List<Patient>();

            if (!await _context.Patients.AnyAsync(p => p.FullName == "Test Patient UnderObservation"))
            {
                patientsToAdd.Add(new Patient
                {
                    FullName = "Test Patient UnderObservation",
                    Age = 28,
                    DateOfBirth = new DateTime(1998, 2, 14),
                    AdmittedAt = DateTime.UtcNow,
                    FlowStatus = PatientFlowStatus.UnderObservation,
                    CurrentStatus = PatientStatus.Stable,
                    NewsScore = 4,
                    Background = "No major history",
                    PreviousMedications = "Paracetamol"
                });
            }

            if (!await _context.Patients.AnyAsync(p => p.FullName == "Test Patient WaitingDoctor"))
            {
                patientsToAdd.Add(new Patient
                {
                    FullName = "Test Patient WaitingDoctor",
                    Age = 61,
                    DateOfBirth = new DateTime(1965, 6, 8),
                    AdmittedAt = DateTime.UtcNow,
                    FlowStatus = PatientFlowStatus.WaitingDoctor,
                    CurrentStatus = PatientStatus.Critical,
                    NewsScore = 8,
                    Background = "Hypertension",
                    PreviousMedications = "Aspirin, Metformin"
                });
            }

            if (!await _context.Patients.AnyAsync(p => p.FullName == "Test Patient ObservationalStable"))
            {
                patientsToAdd.Add(new Patient
                {
                    FullName = "Test Patient ObservationalStable",
                    Age = 36,
                    DateOfBirth = new DateTime(1990, 10, 1),
                    AdmittedAt = DateTime.UtcNow,
                    FlowStatus = PatientFlowStatus.ObservationalStable,
                    CurrentStatus = PatientStatus.Stable,
                    NewsScore = 1,
                    Background = "Mild asthma",
                    PreviousMedications = "Ventolin"
                });
            }

            if (!await _context.Patients.AnyAsync(p => p.FullName == "Test Patient UnderTreatment"))
            {
                patientsToAdd.Add(new Patient
                {
                    FullName = "Test Patient UnderTreatment",
                    Age = 49,
                    DateOfBirth = new DateTime(1977, 12, 20),
                    AdmittedAt = DateTime.UtcNow,
                    FlowStatus = PatientFlowStatus.UnderTreatment,
                    CurrentStatus = PatientStatus.Critical,
                    NewsScore = 5,
                    Background = "COPD",
                    PreviousMedications = "Salbutamol",
                    CurrentTreatment = "Ceftriaxone 1g IV daily"
                });
            }

            if (patientsToAdd.Any())
            {
                await _context.Patients.AddRangeAsync(patientsToAdd);
                await _context.SaveChangesAsync();
            }
        }
    }
}