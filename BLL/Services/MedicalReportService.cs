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

    public async Task CreateAsync(CreateMedicalReportDto dto, string doctorId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == dto.PatientId);

        if (patient == null)
            throw new Exception("Patient not found");

        var doctor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctorId);

        if (doctor == null)
            throw new Exception("Doctor not found");

        switch (dto.ReportType)
        {
            case MedicalReportType.TreatmentPlan:
                if (patient.FlowStatus != PatientFlowStatus.WaitingDoctor &&
                    patient.FlowStatus != PatientFlowStatus.ObservationalStable)
                    throw new Exception(
                        $"Cannot create TreatmentPlan. Patient must be in WaitingDoctor or ObservationalStable status. Current: {patient.FlowStatus}");

                patient.CurrentTreatment = dto.TreatmentPlan;
                break;

            case MedicalReportType.DischargeReport:
                if (patient.FlowStatus != PatientFlowStatus.WaitingDoctor &&
                    patient.FlowStatus != PatientFlowStatus.ObservationalStable)
                    throw new Exception(
                        $"Cannot create DischargeReport. Patient must be in WaitingDoctor or ObservationalStable status. Current: {patient.FlowStatus}");
                break;
        }

        var report = new MedicalReport
        {
            PatientId = dto.PatientId,
            DoctorId = doctorId,
            Diagnosis = dto.Diagnosis,
            TreatmentPlan = dto.TreatmentPlan,
            ReportType = dto.ReportType
        };

        await _context.MedicalReports.AddAsync(report);
        await _context.SaveChangesAsync();

        switch (dto.ReportType)
        {
            case MedicalReportType.TreatmentPlan:
                await _workflowService.AdvanceAsync(dto.PatientId, PatientFlowStatus.UnderTreatment);
                break;

            case MedicalReportType.DischargeReport:
                await _workflowService.AdvanceAsync(dto.PatientId, PatientFlowStatus.Stable);
                break;
        }
    }

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