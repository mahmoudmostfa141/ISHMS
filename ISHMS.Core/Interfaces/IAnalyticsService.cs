using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.DTOs.Analytics;

namespace ISHMS.Core.Interfaces
{
    public interface IAnalyticsService
    {
        Task<ExecutiveSummaryDto> ExecutiveSummary();
        Task<List<DepartmentLoadDto>> DepartmentLoad();
        Task<List<AlertTrendDto>> AlertTrend(int days = 7);
        Task<List<SlaComplianceDto>> SlaCompliance();
        Task<List<RiskBoardDto>> RiskBoard(string? department = null,string? riskBand = null);
        Task<VitalTrendDto> VitalTrend(int patientId,int hours = 24);
        Task<List<AlertFeedDto>> AlertFeed(int limit = 20);
        Task<List<EscalationDto>> Escalations();
        Task<BedMapDto> BedMap(string? department = null);
        Task<List<PeakHourDto>> PeakHours();
        Task<List<BedShortageRiskDto>>BedShortageRisk();
    }
}
