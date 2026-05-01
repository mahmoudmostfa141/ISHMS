using ISHMS.Core.DTOs;

namespace ISHMS.Core.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAll();
}