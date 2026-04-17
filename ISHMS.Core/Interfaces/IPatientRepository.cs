using ISHMS.Core.Models;

namespace ISHMS.Core.Interfaces;

public interface IPatientRepository
{
    Task<List<Patient>> GetAllAsync();
    Task<Patient?> GetByIdAsync(int id);
    Task AddAsync(Patient patient);
    Task SaveAsync();
}