using ISHMS.Core.Enums;

namespace ISHMS.Core.Models;

public class WaitingPatient
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; }

    public PriorityLevel Priority { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}