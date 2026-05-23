using ISHMS.Core.DTOs.Drug;

namespace ISHMS.Core.Interfaces;

public interface IDrugInteractionService
{
    Task<List<DrugInteractionResultDto>> CheckAsync(
        List<string> currentMedications,
        string newMedication);
}