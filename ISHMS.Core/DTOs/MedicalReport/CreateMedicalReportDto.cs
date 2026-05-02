using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.Enums;

namespace ISHMS.Core.DTOs.MedicalReport
{
    public class CreateMedicalReportDto
    {
        public int PatientId { get; set; }
        public string DoctorId { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentPlan { get; set; } = string.Empty;
        public MedicalReportType ReportType { get; set; }
    }
}
