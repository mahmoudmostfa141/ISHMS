using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.MedicalReport;
using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class MedicalReportService : IMedicalReportService
{
    private readonly AppDbContext _context;
    private readonly IWorkflowService _workflowService;


    public MedicalReportService(
        AppDbContext context,
        IWorkflowService workflowService)
    {
        _context = context;
        _workflowService = workflowService;
    }

    // ==================== Create ====================

    public async Task CreateAsync(CreateMedicalReportDto dto)
    {
        // Validation
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == dto.PatientId);

        if (patient == null)
            throw new Exception("Patient not found");

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == dto.DoctorId);

        if (doctor == null)
            throw new Exception("Doctor not found");

        var report = new MedicalReport
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            Diagnosis = dto.Diagnosis,
            TreatmentPlan = dto.TreatmentPlan,
            ReportType = dto.ReportType
        };

        switch (dto.ReportType)
        {
            case MedicalReportType.TreatmentPlan:
                // Doctor يقدر يكتب Treatment لو المريض في WaitingDoctor أو ObservationalStable
                if (patient.FlowStatus != PatientFlowStatus.WaitingDoctor &&
                    patient.FlowStatus != PatientFlowStatus.ObservationalStable)
                    throw new Exception(
                        $"Cannot create TreatmentPlan. " +
                        $"Patient must be in WaitingDoctor or ObservationalStable status. " +
                        $"Current: {patient.FlowStatus}");
                break;

            case MedicalReportType.DischargeReport:
                // Doctor يقدر يقول Stable لو المريض في WaitingDoctor أو ObservationalStable
                if (patient.FlowStatus != PatientFlowStatus.WaitingDoctor &&
                    patient.FlowStatus != PatientFlowStatus.ObservationalStable)
                    throw new Exception(
                        $"Cannot create DischargeReport. " +
                        $"Patient must be in WaitingDoctor or ObservationalStable status. " +
                        $"Current: {patient.FlowStatus}");
                break;
        }

        await _context.MedicalReports.AddAsync(report);
        await _context.SaveChangesAsync();

        // ✅ Move flow base on report type
        
        switch (dto.ReportType)
        {
            case MedicalReportType.TreatmentPlan:
                await _workflowService.AdvanceAsync(
                    dto.PatientId,
                    PatientFlowStatus.UnderTreatment);
                break;

            case MedicalReportType.DischargeReport:
                await _workflowService.AdvanceAsync(
                    dto.PatientId,
                    PatientFlowStatus.Stable);
                break;
        }
    }

    // ==================== Get By Patient ====================

    public async Task<List<MedicalReportResponseDto>> GetByPatientAsync(int patientId)
    {
        return await _context.MedicalReports
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync();
    }

    // ==================== Get By Doctor ====================

    public async Task<List<MedicalReportResponseDto>> GetByDoctorAsync(string doctorId)
    {
        return await _context.MedicalReports
            .Include(r => r.Patient)
            .Include(r => r.Doctor)
            .Where(r => r.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToDto(r))
            .ToListAsync();
    }

    // ==================== Mapper ====================

    private static MedicalReportResponseDto ToDto(MedicalReport r)
    {
        return new MedicalReportResponseDto
        {
            Id = r.Id,
            PatientId = r.PatientId,
            PatientName = r.Patient.FullName,
            DoctorName = r.Doctor.FullName,
            Diagnosis = r.Diagnosis,
            TreatmentPlan = r.TreatmentPlan,
            ReportType = r.ReportType.ToString(),
            CreatedAt = r.CreatedAt
        };
    }
}