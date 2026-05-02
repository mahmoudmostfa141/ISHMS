using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.MedicalReport;
using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicalReportController : ControllerBase
{
    private readonly IMedicalReportService _reportService;

    public MedicalReportController(IMedicalReportService reportService)
    {
        _reportService = reportService;
    }

    // POST api/medicalreport
    // الـ Doctor بيكتب Report — نوعه بيحدد هيتحول لـ Treatment ولا Stable
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMedicalReportDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _reportService.CreateAsync(dto);
        return Ok("Medical report created successfully");
    }

    // GET api/medicalreport/patient/{patientId}
    // شوف كل Reports مريض معين
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var result = await _reportService.GetByPatientAsync(patientId);
        return Ok(result);
    }

    // GET api/medicalreport/doctor/{doctorId}
    // Doctor يشوف كل Reports كتبها
    [HttpGet("doctor/{doctorId}")]
    public async Task<IActionResult> GetByDoctor(string doctorId)
    {
        var result = await _reportService.GetByDoctorAsync(doctorId);
        return Ok(result);
    }
}