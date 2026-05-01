using ISHMS.Core.Constants.Enums;
using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Patient;
using ISHMS.Core.Models;

namespace ISHMS.BLL;

public static class PatientMapper
{
    // ✅ Receptionist — بيانات أساسية فقط
    public static Patient ToEntity(CreatePatientDto dto) => new()
    {
        FullName = dto.FullName,
        Age = dto.Age,
        DateOfBirth = dto.DateOfBirth,
        AdmittedAt = DateTime.UtcNow,
        BedId = dto.BedId,
        CurrentStatus = PatientStatus.Stable,
        NewsScore = 0
    };

    // ✅ Response — كل البيانات
    public static PatientResponseDto ToDto(Patient p) => new()
    {
        Id = p.Id,
        FullName = p.FullName,
        Age = p.Age,
        DateOfBirth = p.DateOfBirth,
        AdmittedAt = p.AdmittedAt,
        Status = p.CurrentStatus.ToString(),
        NewsScore = p.NewsScore,
        Background = p.Background,
        PreviousMedications = p.PreviousMedications,
        CurrentTreatment = p.CurrentTreatment,
        BedId = p.BedId,
        VitalSigns = p.VitalSigns?.Select(v => new VitalSignDto
        {
            HeartRate = v.HeartRate,
            OxygenLevel = v.OxygenLevel,
            Temperature = v.Temperature,
            SystolicPressure = v.SystolicPressure,
            DiastolicPressure = v.DiastolicPressure,
            RespirationRate = v.RespirationRate,
            RecordedAt = v.RecordedAt
        }).ToList()
    };
}