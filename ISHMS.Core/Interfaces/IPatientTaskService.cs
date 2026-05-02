using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Task;

namespace ISHMS.Core.Interfaces;

public interface IPatientTaskService
{
    Task CreateAsync(CreatePatientTaskDto dto);
    Task<List<PatientTaskResponseDto>> GetByPatientAsync(int patientId);
    Task<List<PatientTaskResponseDto>> GetByRoleAsync(string role);
    Task<List<PatientTaskResponseDto>> GetByUserAsync(string userId);  // ✅ User-based
    Task CompleteAsync(int taskId);
}