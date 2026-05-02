using ISHMS.Core.Enums;

namespace ISHMS.Core.Interfaces;

public interface IWorkflowService
{
    Task AdvanceAsync(int patientId, PatientFlowStatus newStatus);
    Task<PatientFlowStatus> GetCurrentStatusAsync(int patientId);
}