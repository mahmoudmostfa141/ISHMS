using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Drug;
using ISHMS.Core.DTOs.Patient;

namespace ISHMS.Core.Interfaces;

public interface IPatientService
{
    // Receptionist
    Task<PatientResponseDto> Create(CreatePatientDto dto);

    // All
    Task<List<PatientResponseDto>> GetAll();
    Task<PatientResponseDto?> GetById(int id);
    Task Delete(int id);

    // Nurse
    Task AddVital(CreateVitalDto dto);
    Task UpdateNurseInfo(int id, UpdateNurseDto dto);

    // Doctor
    Task UpdateDoctorInfo(int id, UpdateDoctorDto dto);

    Task DischargeAsync(int patientId);

    //Drug
    Task<List<DrugInteractionResultDto>> CheckDrugInteraction(int patientId);

}