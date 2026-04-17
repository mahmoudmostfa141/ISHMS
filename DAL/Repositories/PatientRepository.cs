using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using ISHMS.DAL.DbContext;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.DAL.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    public PatientRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        return await _context.Patients
            .Include(x => x.VitalSigns)
            .ToListAsync();
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        return await _context.Patients
            .Include(x => x.VitalSigns)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task AddAsync(Patient patient)
    {
        await _context.Patients.AddAsync(patient);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}