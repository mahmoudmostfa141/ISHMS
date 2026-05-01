

namespace ISHMS.Core.DTOs.Patient;
public class UpdateNurseDto
{
    public int PatientId { get; set; }
    public string? Background { get; set; }
    public string? PreviousMedications { get; set; }
}
