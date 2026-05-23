// OccupiedBedDto.cs
namespace ISHMS.Core.DTOs.DepartmentBed;

public class OccupiedBedDto
{
    public int BedId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    // بيانات المريض
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string FlowStatus { get; set; } = string.Empty;
    public int NewsScore { get; set; }
    public DateTime AdmittedAt { get; set; }
}