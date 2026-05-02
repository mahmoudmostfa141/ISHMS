using ISHMS.Core.Enums;

namespace ISHMS.Core.Models;

public class MedicalReport
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; }

    // الـ Doctor اللي كتب التقرير
    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser Doctor { get; set; }

    public string Diagnosis { get; set; } = string.Empty;

    public string TreatmentPlan { get; set; } = string.Empty;

    public MedicalReportType ReportType { get; set; }
    // Treatment أو Discharge

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}