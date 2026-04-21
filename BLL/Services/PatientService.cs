using ISHMS.Core.DTOs;
using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;



namespace ISHMS.BLL.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _context;

    public PatientService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PatientResponseDto> Create(CreatePatientDto dto)
    {
        var patient = PatientMapper.ToEntity(dto);
        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        return PatientMapper.ToDto(patient);
    }

    public async Task<List<PatientResponseDto>> GetAll()
    {
        var data = await _context.Patients.ToListAsync();
        return data.Select(PatientMapper.ToDto).ToList();
    }

    public async Task<PatientResponseDto?> GetById(int id)
    {
        var p = await _context.Patients.FindAsync(id);
        return p == null ? null : PatientMapper.ToDto(p);
    }

    public async Task Update(int id, UpdatePatientDto dto)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Not Found");

        p.FullName = dto.FullName;
        p.Age = dto.Age;
        p.DateOfBirth = dto.DateOfBirth;

        await _context.SaveChangesAsync();
    }

    public async Task Delete(int id)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Not Found");

        _context.Patients.Remove(p);
        await _context.SaveChangesAsync();
    }

    public async Task AddVital(CreateVitalDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null) throw new Exception("Not Found");

        var vital = new VitalSign
        {
            PatientId = dto.PatientId,
            HeartRate = dto.HeartRate,
            OxygenLevel = dto.OxygenLevel,
            Temperature = dto.Temperature,
            SystolicPressure = dto.SystolicPressure,
            DiastolicPressure = dto.DiastolicPressure,
            RespirationRate = dto.RespirationRate
        };

        // 🔥 NEWS Logic بسيط
        int score = 0;
        if (dto.OxygenLevel < 90) score += 3;
        if (dto.HeartRate > 120) score += 2;

        patient.CurrentStatus =
            score >= 5 ? PatientStatus.Critical :
            score >= 2 ? PatientStatus.Unstable :
            PatientStatus.Stable;

        await _context.VitalSigns.AddAsync(vital);
        await _context.SaveChangesAsync();
    }
}