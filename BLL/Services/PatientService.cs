using System.Text.Json.Serialization;
using ISHMS.Core.Constants.Enums;
using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Drug;
using ISHMS.Core.DTOs.Patient;
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
    private readonly IWorkflowService _workflowService;
    private readonly IDrugInteractionService _drugService;

    public PatientService(
        AppDbContext context,
        NewsService newsService,
        IWorkflowService workflowService,
        IDrugInteractionService drugService)
    {
        _context = context;
        _newsService = newsService;
        _workflowService = workflowService;
        _drugService = drugService;
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
        patient.FlowStatus = PatientFlowStatus.New;
        patient.NewsScore = 0;

        await _context.Patients.AddAsync(patient);
        await _context.SaveChangesAsync();

        // ✅ ربط السرير بالمريض بعد ما اتعمل الـ Id
        if (dto.BedId > 0)
        {
            var bed = await _context.Beds.FindAsync(dto.BedId);
            if (bed != null)
            {
                bed.PatientId = patient.Id;
                await _context.SaveChangesAsync();
            }
        }

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
        var patient = await _context.Patients
            .Include(p => p.VitalSigns)
            .FirstOrDefaultAsync(p => p.Id == id);

        return patient == null ? null : PatientMapper.ToDto(patient);
    }

    // ✅ Delete — تحرير السرير تلقائياً
    public async Task Delete(int id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) throw new Exception("Patient not found");

        if (patient.BedId.HasValue)
        {
            var bed = await _context.Beds.FindAsync(patient.BedId.Value);
            if (bed != null)
            {
                bed.IsOccupied = false;
                bed.PatientId = null;
            }
        }

        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();
    }

    // ✅ Nurse — تحديث Background والأدوية السابقة
    public async Task UpdateNurseInfo(int id, UpdateNurseDto dto)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) throw new Exception("Patient not found");

        patient.Background = dto.Background;
        patient.PreviousMedications = dto.PreviousMedications;

        await _context.SaveChangesAsync();
    }

    // ✅ Nurse — إضافة VitalSigns + حساب NEWS + Workflow
    public async Task AddVital(CreateVitalDto dto)
    {
        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null) throw new Exception("Patient not found");

        // ✅ حفظ الـ Vital
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

        // ✅ NEWS Calculation
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
        await _context.Entry(patient).ReloadAsync();
        // ✅ Workflow Logic بناءً على FlowStatus + NEWS Score
        var currentFlow = patient.FlowStatus;

        if (currentFlow == PatientFlowStatus.New)
        {
            // أول Vitals دايماً → UnderObservation
            await _workflowService.AdvanceAsync(
                patient.Id,
                PatientFlowStatus.UnderObservation);
        }
        else if (currentFlow == PatientFlowStatus.UnderObservation)
        {
            if (result.score >= 7)
            {
                // Red → WaitingDoctor
                await _workflowService.AdvanceAsync(
                    patient.Id,
                    PatientFlowStatus.WaitingDoctor);
            }
            else if (result.score <= 2)
            {
                // Green → ObservationalStable
                await _workflowService.AdvanceAsync(
                    patient.Id,
                    PatientFlowStatus.ObservationalStable);
            }
            // Yellow (3-6) → يفضل UnderObservation
        }
        else if (currentFlow == PatientFlowStatus.ObservationalStable && result.score >= 7)
        {
            // كان Green وحالته اتدهورت → WaitingDoctor
            await _workflowService.AdvanceAsync(
                patient.Id,
                PatientFlowStatus.WaitingDoctor);
        }
        else if (currentFlow == PatientFlowStatus.Stable && result.score >= 7)
        {
            // كان Stable وحالته اتدهورت → WaitingDoctor
            await _workflowService.AdvanceAsync(
                patient.Id,
                PatientFlowStatus.WaitingDoctor);
        }
    }

    // ✅ Doctor — تحديث العلاج الحالي
    public async Task UpdateDoctorInfo(int id, UpdateDoctorDto dto)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient == null) throw new Exception("Patient not found");

        patient.CurrentTreatment = dto.CurrentTreatment;

        await _context.SaveChangesAsync();
    }

    // ✅ Receptionist — تخليص المريض
    public async Task DischargeAsync(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) throw new Exception("Patient not found");

        if (patient.FlowStatus != PatientFlowStatus.Stable)
            throw new Exception(
                $"Cannot discharge patient. Current status: {patient.FlowStatus}. " +
                $"Patient must be Stable first.");

        await _workflowService.AdvanceAsync(patientId, PatientFlowStatus.Discharged);

        // ✅ تحرير السرير
        var bed = await _context.Beds
            .FirstOrDefaultAsync(b => b.PatientId == patientId);

        if (bed != null)
        {
            bed.IsOccupied = false;
            bed.PatientId = null;
            await _context.SaveChangesAsync();
        }
    }

    // ✅ Doctor — فحص تفاعل الأدوية السابقة مع العلاج الجديد
    public async Task<List<DrugInteractionResultDto>> CheckDrugInteraction(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);
        if (patient == null) throw new Exception("Patient not found");

        if (string.IsNullOrWhiteSpace(patient.PreviousMedications))
            throw new Exception("No previous medications recorded for this patient");

        if (string.IsNullOrWhiteSpace(patient.CurrentTreatment))
            throw new Exception("Doctor has not added current treatment yet");

        // "warfarin, metformin" → ["warfarin", "metformin"]
        var currentMeds = patient.PreviousMedications
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .ToList();

        return await _drugService.CheckAsync(currentMeds, patient.CurrentTreatment);
    }
}