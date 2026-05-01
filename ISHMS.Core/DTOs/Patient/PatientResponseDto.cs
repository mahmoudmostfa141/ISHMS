namespace ISHMS.Core.DTOs.Patient;

public class PatientResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public int Age { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime AdmittedAt { get; set; }
    public string Status { get; set; }
    public int NewsScore { get; set; }
    public string? Background { get; set; }
    public string? PreviousMedications { get; set; }
    public string? CurrentTreatment { get; set; }
    public int? BedId { get; set; }
    public List<VitalSignDto>? VitalSigns { get; set; }
}

public class VitalSignDto
{
    public int HeartRate { get; set; }
    public double OxygenLevel { get; set; }
    public double Temperature { get; set; }
    public int SystolicPressure { get; set; }
    public int DiastolicPressure { get; set; }
    public int RespirationRate { get; set; }
    public DateTime RecordedAt { get; set; }
}