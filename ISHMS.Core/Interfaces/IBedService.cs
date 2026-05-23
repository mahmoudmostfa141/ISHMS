

using ISHMS.Core.DTOs.DepartmentBed;

namespace ISHMS.Core.Interfaces;
public interface IBedService
{
    Task AssignPatient(AssignBedDto dto);
    Task<List<AvailableBedDto>> GetAvailableBeds();
    Task<List<AvailableBedDto>> GetAvailableBedsByDepartment(int departmentId);
    Task<List<OccupiedBedDto>> GetOccupiedBeds();
}
