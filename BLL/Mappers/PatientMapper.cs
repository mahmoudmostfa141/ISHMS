using ISHMS.Core.DTOs;
using ISHMS.Core.Models;

namespace ISHMS.BLL;

public static class PatientMapper
{
    public static Patient ToEntity(CreatePatientDto dto)
    {
        return new Patient
        {
            FullName = dto.FullName,
            Age = dto.Age,
            DateOfBirth = dto.DateOfBirth,
            AdmittedAt = DateTime.Now
        };
    }

    public static PatientResponseDto ToDto(Patient p)
    {
        return new PatientResponseDto
        {
            Id = p.Id,
            FullName = p.FullName,
            Age = p.Age,
            Status = p.CurrentStatus.ToString()
        };
    }
}