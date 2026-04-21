using ISHMS.Core.DTOs;

namespace ISHMS.Core.Interfaces;

public interface IPatientService
{
    Task<PatientResponseDto> Create(CreatePatientDto dto);
    Task<List<PatientResponseDto>> GetAll();
    Task<PatientResponseDto?> GetById(int id);
    Task Update(int id, UpdatePatientDto dto);
    Task Delete(int id);
    Task AddVital(CreateVitalDto dto);
}