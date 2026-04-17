using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using ISHMS.DAL.DbContext;
using System;

namespace ISHMS.BLL.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _repository;
    private readonly AppDbContext _context;

    public PatientService(IPatientRepository repository, AppDbContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<Patient> CreatePatientAsync(Patient patient)
    {
        await _repository.AddAsync(patient);
        await _repository.SaveAsync();
        return patient;
    }

    public async Task<VitalSign> AddVitalSignAsync(VitalSign vitalSign)
    {
        var patient = await _repository.GetByIdAsync(vitalSign.PatientId);

        if (patient == null)
            throw new Exception("Patient not found");

        int score = 0;

        if (vitalSign.HeartRate < 50 || vitalSign.HeartRate > 120)
            score += 2;

        if (vitalSign.OxygenLevel < 90)
            score += 3;

        if (vitalSign.Temperature > 39)
            score += 2;

        if (vitalSign.RespirationRate > 30)
            score += 2;

        patient.CurrentStatus =
            score >= 5 ? PatientStatus.Critical :
            score >= 2 ? PatientStatus.Unstable :
            PatientStatus.Stable;

        await _context.VitalSigns.AddAsync(vitalSign);
        await _context.SaveChangesAsync();

        return vitalSign;
    }
}