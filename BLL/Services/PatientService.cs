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
    private readonly IWorkflowService _workflowService;
    public PatientService(AppDbContext context, NewsService newsService, IWorkflowService workflowService )
    {
        _context = context;
        _newsService = newsService;
        _workflowService = workflowService;
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

        await _context.SaveChangesAsync();

        // ✅ Workflow Logic
        if (patient.FlowStatus == PatientFlowStatus.New)
        {
            // أول Vitals دايماً → UnderObservation
            await _workflowService.AdvanceAsync(
                patient.Id,
                PatientFlowStatus.UnderObservation);
        }
        else if (patient.FlowStatus == PatientFlowStatus.UnderObservation)
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
            // Yellow (3-6)  UnderObservation
        }
        else if (patient.FlowStatus == PatientFlowStatus.ObservationalStable
                 && result.score >= 7)
        {
            // كان Green وحالته اتدهورت → WaitingDoctor
            await _workflowService.AdvanceAsync(
                patient.Id,
                PatientFlowStatus.WaitingDoctor);
        }

        // ✅ Bed Logic
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
    public async Task DischargeAsync(int patientId)
    {
        var patient = await _context.Patients.FindAsync(patientId);

        if (patient == null)
            throw new Exception("Patient not found");

        if (patient.FlowStatus != PatientFlowStatus.Stable)
            throw new Exception(
                $"Cannot discharge patient. Current status: {patient.FlowStatus}. Patient must be Stable first.");

        await _workflowService.AdvanceAsync(
            patientId,
            PatientFlowStatus.Discharged);

        var bed = await _context.Beds
            .FirstOrDefaultAsync(b => b.PatientId == patientId);

        if (bed != null)
        {
            bed.IsOccupied = false;
            bed.PatientId = null;
            await _context.SaveChangesAsync();
        }
    }
}