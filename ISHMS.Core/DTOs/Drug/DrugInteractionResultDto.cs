using System.Text.Json.Serialization;

namespace ISHMS.Core.DTOs.Drug;

public class DrugInteractionResultDto
{
    [JsonPropertyName("drug_pair")]
    public string DrugPair { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("risk_summary")]
    public string RiskSummary { get; set; } = string.Empty;
}