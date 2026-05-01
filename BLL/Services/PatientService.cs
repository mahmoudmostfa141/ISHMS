using ISHMS.Core.Constants.Enums;
using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Patient;
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

    // ✅ Receptionist — Create Patient + Assign Bed
    public async Task<PatientResponseDto> Create(CreatePatientDto dto)
    {
        if (dto.BedId > 0)
        {
            var bed = await _context.Beds
                .FirstOrDefaultAsync(b => b.Id == dto.BedId);

            if (bed == null) throw new Exception("Bed not found");
            if (bed.IsOccupied) throw new Exception("Bed is already occupied");
            bed.IsOccupied = true;
        }

        var patient = PatientMapper.ToEntity(dto);
        patient.CurrentStatus = PatientStatus.Stable;
        patient.NewsScore = 0;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        return PatientMapper.ToDto(patient);
    }

    // ✅ Get All
    public async Task<List<PatientResponseDto>> GetAll()
    {
        var data = await _context.Patients
            .Include(p => p.VitalSigns)
            .ToListAsync();
        return data.Select(PatientMapper.ToDto).ToList();
    }

    // ✅ Get By Id
    public async Task<PatientResponseDto?> GetById(int id)
    {
        var p = await _context.Patients
            .Include(p => p.VitalSigns)
            .FirstOrDefaultAsync(p => p.Id == id);
        return p == null ? null : PatientMapper.ToDto(p);
    }

    // ✅ Delete
    public async Task Delete(int id)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Patient Not Found");

        // تحرير السرير عند الحذف
        if (p.BedId.HasValue)
        {
            var bed = await _context.Beds.FindAsync(p.BedId.Value);
            if (bed != null) bed.IsOccupied = false;
        }

        _context.Patients.Remove(p);
        await _context.SaveChangesAsync();
    }

    // ✅ Nurse — تحديث Background والأدوية القديمة
    public async Task UpdateNurseInfo(int id, UpdateNurseDto dto)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Patient Not Found");

        p.Background = dto.Background;
        p.PreviousMedications = dto.PreviousMedications;

        await _context.SaveChangesAsync();
    }

    // ✅ Nurse — إضافة VitalSigns + حساب NEWS
    public async Task AddVital(CreateVitalDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null) throw new Exception("Patient Not Found");

        var vital = new VitalSign
        {
            PatientId = dto.PatientId,
            HeartRate = dto.HeartRate,
            OxygenLevel = dto.OxygenLevel,
            Temperature = dto.Temperature,
            SystolicPressure = dto.SystolicPressure,
            DiastolicPressure = dto.DiastolicPressure,
            RespirationRate = dto.RespirationRate,
            RecordedAt = DateTime.UtcNow
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

        patient.NewsScore = result.score;
        patient.CurrentStatus = result.status;

        await _context.SaveChangesAsync();
    }

    // ✅ Doctor — تحديث العلاج الحالي
    public async Task UpdateDoctorInfo(int id, UpdateDoctorDto dto)
    {
        var p = await _context.Patients.FindAsync(id);
        if (p == null) throw new Exception("Patient Not Found");

        p.CurrentTreatment = dto.CurrentTreatment;

        await _context.SaveChangesAsync();
    }
}