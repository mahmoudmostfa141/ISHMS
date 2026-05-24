using ISHMS.Core.DTOs.MedicalReport;

namespace ISHMS.Core.Interfaces
{
    public interface IMedicalReportService
    {
        Task CreateAsync(CreateMedicalReportDto dto, string doctorId);
        Task<List<MedicalReportResponseDto>> GetByPatientAsync(int patientId);
        Task<List<MedicalReportResponseDto>> GetByDoctorAsync(string doctorId);
    }
}