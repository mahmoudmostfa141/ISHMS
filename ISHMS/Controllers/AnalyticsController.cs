using ISHMS.BLL.Services;
using ISHMS.Core.DTOs.Analytics;
using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _service;

        public AnalyticsController(IAnalyticsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> ExecutiveSummary()
        {
            ExecutiveSummaryDto summary = await _service.ExecutiveSummary();
            return Ok(summary);
        }

        [HttpGet("executive/department-load")]
        public async Task<IActionResult> DepartmentLoad()
        {
            var load = await _service.DepartmentLoad();
            return Ok(load);
        }
        [HttpGet("executive/alert-trend")]
        public async Task<IActionResult> AlertTrend([FromQuery] int days = 7)
        {
            return Ok(
                await _service.AlertTrend(days));
        }
        [HttpGet("executive/sla-compliance")]
        public async Task<IActionResult> SlaCompliance()
        {
            var result = await _service.SlaCompliance();

            return Ok(result);
        }
        [HttpGet("clinical/risk-board")]
        public async Task<IActionResult> RiskBoard([FromQuery] string? department, [FromQuery] string? riskBand)
        {
            var result =
                await _service
                .RiskBoard(
                    department,
                    riskBand);

            return Ok(result);
        }
        [HttpGet("clinical/vital-trend/{patientId}")]
        public async Task<IActionResult> VitalTrend(int patientId, [FromQuery] int hours = 24)
        {
            var result =
                await _service
                .VitalTrend(patientId, hours);

            return Ok(result);
        }
        [HttpGet("clinical/alert-feed")]
        public async Task<IActionResult> AlertFeed([FromQuery] int limit = 20)
        {
            var result =
                await _service
                .AlertFeed(limit);

            return Ok(result);
        }
        [HttpGet("clinical/escalations")]
        public async Task<IActionResult>Escalations()
        {
            var result = await _service.Escalations();

            return Ok(result);
        }
        [HttpGet("operations/bed-map")]
        public async Task<IActionResult>BedMap([FromQuery]string? department)
        {
            var result =
                await _service
                .BedMap(department);

            return Ok(result);
        }
        [HttpGet("operations/peak-hours")]
        public async Task<IActionResult>PeakHours()
        {
            return Ok(
                await _service
                .PeakHours());
        }
        [HttpGet("operations/bed-shortage-risk")]
        public async Task<IActionResult>BedShortageRisk()
        {
            return Ok(
                await _service
                .BedShortageRisk());
        }
    }
}
