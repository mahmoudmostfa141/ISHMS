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
    private readonly NewsService _newsService;

    public PatientService(AppDbContext context, NewsService newsService)
    {
        _context = context;
        _newsService = newsService;
    }

    // ✅ Create Patient
    public async Task<PatientResponseDto> Create(CreatePatientDto dto)
    {
        var patient = PatientMapper.ToEntity(dto);

        // Default values
        patient.CurrentStatus = PatientStatus.Stable;
        patient.Priority = PriorityLevel.Low;
        patient.NewsScore = 0;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        return PatientMapper.ToDto(patient);
    }

    // ✅ Get All
    public async Task<List<PatientResponseDto>> GetAll()
    {
        var data = await _context.Patients.ToListAsync();
        return data.Select(PatientMapper.ToDto).ToList();
    }

    // ✅ Get By Id
    public async Task<PatientResponseDto?> GetById(int id)
    {
        var p = await _context.Patients.FindAsync(id);
        return p == null ? null : PatientMapper.ToDto(p);
    }

    // ✅ Update
    public async Task Update(int id, UpdatePatientDto dto)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Patient Not Found");

        p.FullName = dto.FullName;
        p.Age = dto.Age;
        p.DateOfBirth = dto.DateOfBirth;

        await _context.SaveChangesAsync();
    }

    // ✅ Delete
    public async Task Delete(int id)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Patient Not Found");

        _context.Patients.Remove(p);
        await _context.SaveChangesAsync();
    }

    // 🔥 Add Vital + NEWS + Priority + Bed Logic
    public async Task AddVital(CreateVitalDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null) throw new Exception("Patient Not Found");

        // ✅ Save Vital
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

        await _context.VitalSigns.AddAsync(vital);

        // 🔥 NEWS Calculation
        var result = _newsService.Calculate(
            dto.HeartRate,
            dto.OxygenLevel,
            dto.Temperature,
            dto.SystolicPressure,
            dto.RespirationRate
        );

        // ✅ Update Patient حالة
        patient.NewsScore = result.score;
        patient.CurrentStatus = result.status;
        patient.Priority = result.priority;

        // 🔥 Check Available Bed
        var freeBed = await _context.Beds
            .FirstOrDefaultAsync(b => !b.IsOccupied);

        if (freeBed != null)
        {
            // ✅ Assign Bed
            freeBed.IsOccupied = true;
            freeBed.PatientId = patient.Id;
        }
        else
        {
            // 🔥 Add to Waiting List
            var alreadyWaiting = await _context.WaitingPatients
                .AnyAsync(w => w.PatientId == patient.Id);

            if (!alreadyWaiting)
            {
                var waiting = new WaitingPatient
                {
                    PatientId = patient.Id,
                    Priority = patient.Priority
                };

                await _context.WaitingPatients.AddAsync(waiting);
            }
        }

        await _context.SaveChangesAsync();
    }
}