using ISHMS.Core.Enums;

namespace ISHMS.Core.DTOs.MedicalReport
{
    public class CreateMedicalReportDto
    {
        public int PatientId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentPlan { get; set; } = string.Empty;
        public MedicalReportType ReportType { get; set; }
    }
}