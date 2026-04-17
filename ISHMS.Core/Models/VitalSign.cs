using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ISHMS.Core.Models;

public class VitalSign
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [ForeignKey("PatientId")]
    public Patient Patient { get; set; }

    [Range(30, 200)]
    public int HeartRate { get; set; }

    [Range(50, 100)]
    public int OxygenLevel { get; set; }

    [Range(30, 45)]
    public double Temperature { get; set; }

    [Range(50, 250)]
    public int SystolicPressure { get; set; }

    [Range(30, 150)]
    public int DiastolicPressure { get; set; }

    [Range(5, 50)]
    public int RespirationRate { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}