using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISHMS.Core.Models;

public class VitalSign
{
    [Key]
    public int Id { get; set; }

    public int PatientId { get; set; }

    [ForeignKey("PatientId")]
    public Patient Patient { get; set; }

    public int HeartRate { get; set; }
    public double OxygenLevel { get; set; }
    public double Temperature { get; set; }

    public int SystolicPressure { get; set; }
    public int DiastolicPressure { get; set; }

    public int RespirationRate { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}