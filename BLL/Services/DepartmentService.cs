using ISHMS.Core.DTOs;
using ISHMS.Core.Interfaces;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _context;

    public DepartmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DepartmentDto>> GetAll()
    {
        return await _context.Departments
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .ToListAsync();
    }
}