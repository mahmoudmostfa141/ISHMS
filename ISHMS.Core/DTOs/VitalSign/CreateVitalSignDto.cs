using System.ComponentModel.DataAnnotations;

namespace ISHMS.Core.DTOs;

public class CreateVitalDto
{
    public int PatientId { get; set; }

    [Range(30, 200)]
    public int HeartRate { get; set; }

    [Range(50, 100)]
    public int OxygenLevel { get; set; }

    public double Temperature { get; set; }

    public int SystolicPressure { get; set; }
    public int DiastolicPressure { get; set; }

    public int RespirationRate { get; set; }
}