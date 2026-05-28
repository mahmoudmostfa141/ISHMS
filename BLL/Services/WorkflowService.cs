using ISHMS.Core.Constants;
using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Alert;
using ISHMS.Core.DTOs.Task;
using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class WorkflowService : IWorkflowService
{
    private readonly AppDbContext _context;
    private readonly IPatientTaskService _taskService;
    private readonly IAlertService _alertService;
    private readonly IHubService _hubService;

    public WorkflowService(
        AppDbContext context,
        IPatientTaskService taskService,
        IAlertService alertService,
        IHubService hubService)
    {
        _context = context;
        _taskService = taskService;
        _alertService = alertService;
        _hubService = hubService;
    }

    // ==================== Advance ====================

    public async Task AdvanceAsync(int patientId, PatientFlowStatus newStatus)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            throw new Exception("Patient not found");

        ValidateTransition(patient.FlowStatus, newStatus);

        patient.FlowStatus = newStatus;
        await _context.SaveChangesAsync();

        // ✅ جديد — بعت Status Change للـ Doctors و Nurses
        await _hubService.SendStatusUpdateAsync(
            patient.Id,
            patient.FullName,
            newStatus.ToString()
        );

        await HandleSideEffectsAsync(patient.Id, patient.FullName, newStatus);
    }
    // ==================== Get Current Status ====================

    public async Task<PatientFlowStatus> GetCurrentStatusAsync(int patientId)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            throw new Exception("Patient not found");

        return patient.FlowStatus;
    }

    // ==================== Validate Transition ====================

    private void ValidateTransition(
        PatientFlowStatus current,
        PatientFlowStatus next)
    {
        // كل Status عنده List من الانتقالات المسموح بيها
        var allowedTransitions = new Dictionary<PatientFlowStatus, List<PatientFlowStatus>>
        {
            {
                PatientFlowStatus.New,
                new List<PatientFlowStatus>
                {
                    PatientFlowStatus.UnderObservation
                }
            },
            {
                PatientFlowStatus.UnderObservation,
                new List<PatientFlowStatus>
                {
                    PatientFlowStatus.WaitingDoctor,       // NEWS Red
                    PatientFlowStatus.ObservationalStable  // NEWS Green
                    // Yellow → يفضل UnderObservation (مفيش transition)
                }
            },
            {
                PatientFlowStatus.WaitingDoctor,
                new List<PatientFlowStatus>
                {
                    PatientFlowStatus.UnderTreatment,  // Doctor قرر علاج
                    PatientFlowStatus.Stable           // Doctor قرر Stable مباشرة
                }
            },
            {
                PatientFlowStatus.ObservationalStable,
                new List<PatientFlowStatus>
                {
                    PatientFlowStatus.UnderTreatment,  // Doctor قرر علاج
                    PatientFlowStatus.Stable,          // Doctor قرر Stable
                    PatientFlowStatus.WaitingDoctor    // حالته اتدهورت → NEWS Red
                }
            },
            {
                PatientFlowStatus.UnderTreatment,
                new List<PatientFlowStatus>
                {
                    PatientFlowStatus.Stable  // Doctor قال اتحسن
                }
            },
            {
                PatientFlowStatus.Stable,
                new List<PatientFlowStatus>
                {
                    PatientFlowStatus.Discharged  // Receptionist
                }
            }
        };

        // تأكد إن الـ Current Status عنده transitions 
        if (!allowedTransitions.TryGetValue(current, out var allowed))
            throw new Exception(
                $"Patient is in final status: {current}. No further transitions allowed.");

        // تأكد إن الـ Next Status موجود في الـ List
        if (!allowed.Contains(next))
            throw new Exception(
                $"Invalid transition: {current} → {next}. " +
                $"Allowed transitions: {string.Join(", ", allowed)}");
    }

    // ==================== Side Effects ====================

    private async Task HandleSideEffectsAsync(
     int patientId,
     string patientName,
     PatientFlowStatus newStatus)
    {
        switch (newStatus)
        {
            case PatientFlowStatus.UnderObservation:
                await _taskService.CreateAsync(new CreatePatientTaskDto
                {
                    PatientId = patientId,
                    AssignedToRole = AppRoles.Nurse,
                    Title = "Monitor Patient Vitals",
                    Description = $"Patient {patientName} is now under observation. Monitor and record vitals regularly."
                });
                break;

            case PatientFlowStatus.ObservationalStable:
                await _taskService.CreateAsync(new CreatePatientTaskDto
                {
                    PatientId = patientId,
                    AssignedToRole = AppRoles.Nurse,
                    Title = "Routine Vitals Check",
                    Description = $"Patient {patientName} is observationally stable. Continue routine monitoring."
                });

            

                await _alertService.CreateAsync(new CreateAlertDto
                {
                    PatientId = patientId,
                    TargetRole = AppRoles.Doctor,
                    Message = $"Patient {patientName} is observationally stable. Please review when available.",
                    Severity = AlertSeverity.Info
                });

               
                break;

            case PatientFlowStatus.WaitingDoctor:
                await _taskService.CreateAsync(new CreatePatientTaskDto
                {
                    PatientId = patientId,
                    AssignedToRole = AppRoles.Nurse,
                    Title = "Prepare Patient for Doctor",
                    Description = $"Patient {patientName} NEWS Score is critical. Prepare for immediate doctor examination."
                });

               

                await _alertService.CreateAsync(new CreateAlertDto
                {
                    PatientId = patientId,
                    TargetRole = AppRoles.Doctor,
                    Message = $"URGENT: Patient {patientName} requires immediate attention.",
                    Severity = AlertSeverity.Critical
                });
               
                break;

            case PatientFlowStatus.UnderTreatment:
                await _alertService.CreateAsync(new CreateAlertDto
                {
                    PatientId = patientId,
                    TargetRole = AppRoles.Nurse,
                    Message = $"Treatment plan created for patient {patientName}. Begin treatment protocol.",
                    Severity = AlertSeverity.Warning
                });
              
                break;

            case PatientFlowStatus.Stable:
                await _alertService.CreateAsync(new CreateAlertDto
                {
                    PatientId = patientId,
                    TargetRole = AppRoles.Receptionist,
                    Message = $"Patient {patientName} is stable and ready for discharge procedures.",
                    Severity = AlertSeverity.Info
                });

                break;

            case PatientFlowStatus.Discharged:
                break;
        }
           
    }
}