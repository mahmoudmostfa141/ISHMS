namespace ISHMS.Core.DTOs.Patient;

public class PatientListResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public int Age { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime AdmittedAt { get; set; }
    public string Status { get; set; }
    public int NewsScore { get; set; }
    public string? FlowStatus { get; set; }
    public string? Background { get; set; }
    public string? PreviousMedications { get; set; }
    public string? CurrentTreatment { get; set; }
    public int? BedId { get; set; }
    public VitalSignDto? LatestVitalSign { get; set; }
}