namespace ISHMS.Core.DTOs;

public class UpdatePatientDto
{
    public string FullName { get; set; }
    public int Age { get; set; }
    public DateTime DateOfBirth { get; set; }

    // معلومات إضافية (اختيارية)
    public string? Background { get; set; }
    public string? PreviousMedications { get; set; }
    public string? CurrentTreatment { get; set; }
}