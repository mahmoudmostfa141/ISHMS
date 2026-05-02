namespace ISHMS.Core.Enums;

public enum PatientFlowStatus
{
    New = 1,
    UnderObservation = 2,
    WaitingDoctor = 3,
    UnderTreatment = 4,
    ObservationalStable = 5,  
    Stable = 6,
    Discharged = 7
}