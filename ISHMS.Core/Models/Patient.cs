using ISHMS.Core.Constants.Enums;
using ISHMS.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISHMS.Core.Models;

public class Patient
{
    public int Id { get; set; }

    public string FullName { get; set; }
    public int Age { get; set; }
    public DateTime DateOfBirth { get; set; }

    // ✅ مضافة — كانت مفقودة
    public DateTime AdmittedAt { get; set; } = DateTime.UtcNow;

    // ✅ مضافة — كانت مفقودة
    public PatientStatus CurrentStatus { get; set; } = PatientStatus.Stable;

    // ✅ مضافة — كانت مفقودة
    public int NewsScore { get; set; } = 0;

    // Background & Treatment
    public string? Background { get; set; }
    public string? PreviousMedications { get; set; }
    public string? CurrentTreatment { get; set; }

    // Relations
    public int? BedId { get; set; }

    [ForeignKey("BedId")]
    public Bed? Bed { get; set; }

    // ✅ مضافة — Navigation Property للـ VitalSigns
    public ICollection<VitalSign> VitalSigns { get; set; } = new List<VitalSign>();

    public PatientFlowStatus FlowStatus { get; set; } = PatientFlowStatus.New;

    public ICollection<PatientTask>? Tasks { get; set; }      // ✅ جديد
    public ICollection<Alert>? Alerts { get; set; }           // ✅ جديد
    public ICollection<MedicalReport>? MedicalReports { get; set; } // ✅ جديد
}