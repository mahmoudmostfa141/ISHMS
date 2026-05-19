using System.Text;
using System.Text.Json;
using ISHMS.Core.DTOs.Drug;
using ISHMS.Core.Interfaces;

namespace ISHMS.BLL.Services;

public class DrugInteractionService : IDrugInteractionService
{
    private readonly HttpClient _httpClient;

    // غيّر الـ URL لو اتغير الـ Cloudflare Tunnel
    private const string ApiUrl =
        "https://ishms-api.istabrq.shop/api/v1/drug/check";

    public DrugInteractionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DrugInteractionResultDto>> CheckAsync(
        List<string> currentMedications,
        string newMedication)
    {
        var payload = new
        {
            current_medications = currentMedications,
            new_medication = newMedication
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(ApiUrl, content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var result = JsonSerializer.Deserialize<List<DrugInteractionResultDto>>(
            responseJson, options);

        return result ?? [];
    }
}