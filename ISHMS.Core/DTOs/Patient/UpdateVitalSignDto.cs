namespace ISHMS.Core.DTOs.Patient;
public class UpdateVitalSignDto
{
    public int HeartRate { get; set; }
    public double OxygenLevel { get; set; }
    public double Temperature { get; set; }
    public int SystolicPressure { get; set; }
    public int DiastolicPressure { get; set; }
    public int RespirationRate { get; set; }
}                          