using ISHMS.Core.DTOs.Patient;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class IsbarService
{
    private readonly AppDbContext _context;

    public IsbarService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IsbarResponseDto> GenerateAsync(int patientId)
    {
        var patient = await _context.Patients
            .Include(p => p.VitalSigns)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null)
            throw new Exception("Patient not found");

        var latestVital = patient.VitalSigns
            .OrderByDescending(v => v.RecordedAt)
            .FirstOrDefault();

        if (latestVital == null)
            throw new Exception("No vital signs found for this patient");

        var situation =
            $"Patient_{patient.Id}" +
            $"{(patient.BedId.HasValue ? $" (Bed {patient.BedId})" : "")}, " +
            $"{patient.CurrentStatus}, NEWS {patient.NewsScore}.";

        var background =
            $"Patient is {patient.Age} years old, admitted at {patient.AdmittedAt:yyyy-MM-dd HH:mm}. " +
            $"History: {patient.Background ?? "No background recorded"}. " +
            $"Previous medications: {patient.PreviousMedications ?? "No previous medications recorded"}.";

        var assessment =
            $"HR {latestVital.HeartRate}, BP {latestVital.SystolicPressure}/{latestVital.DiastolicPressure}, " +
            $"O2 Sat {latestVital.OxygenLevel}%, Temp {latestVital.Temperature}C, RR {latestVital.RespirationRate}. " +
            $"Current flow status: {patient.FlowStatus}. Current treatment: {patient.CurrentTreatment ?? "No treatment recorded"}.";

        var recommendation = patient.NewsScore >= 7
            ? "Urgent doctor review is required. Prepare for escalation."
            : patient.NewsScore >= 3
                ? "Continue close monitoring and reassess vital signs."
                : "Continue routine monitoring.";

        return new IsbarResponseDto
        {
            Situation = situation,
            Background = background,
            Assessment = assessment,
            Recommendation = recommendation
        };
    }
}