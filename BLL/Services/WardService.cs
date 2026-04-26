using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class WardService
{
    private readonly AppDbContext _context;

    public WardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ward>> GetAll()
    {
        return await _context.Wards
            .Include(w => w.Rooms)
            .ToListAsync();
    }

    public async Task<Ward> Create(string name)
    {
        var ward = new Ward { Name = name };

        await _context.Wards.AddAsync(ward);
        await _context.SaveChangesAsync();

        return ward;
    }
}