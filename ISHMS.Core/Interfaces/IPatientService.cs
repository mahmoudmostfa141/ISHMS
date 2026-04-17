using ISHMS.Core.Models;

namespace ISHMS.Core.Interfaces;

public interface IPatientService
{
    Task<Patient> CreatePatientAsync(Patient patient);
    Task<VitalSign> AddVitalSignAsync(VitalSign vitalSign);
}