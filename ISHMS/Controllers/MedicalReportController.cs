using System.Security.Claims;
using ISHMS.Core.DTOs.MedicalReport;
using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalReportController : ControllerBase
{
    private readonly IMedicalReportService _reportService;

    public MedicalReportController(IMedicalReportService reportService)
    {
        _reportService = reportService;
    }

    // POST api/MedicalReport
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMedicalReportDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(doctorId))
            return Unauthorized();

        await _reportService.CreateAsync(dto, doctorId);
        return Ok("Medical report created successfully");
    }

    // GET api/MedicalReport/patient/5
    [HttpGet("patient/{patientId}")]
    [Authorize(Roles = "Doctor,Admin,Nurse")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var result = await _reportService.GetByPatientAsync(patientId);
        return Ok(result);
    }

    // GET api/MedicalReport/my-reports
    [HttpGet("my-reports")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetMyReports()
    {
        var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(doctorId))
            return Unauthorized();

        var result = await _reportService.GetByDoctorAsync(doctorId);
        return Ok(result);
    }

    // GET api/MedicalReport/doctor/{doctorId}
    [HttpGet("doctor/{doctorId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetByDoctor(string doctorId)
    {
        var result = await _reportService.GetByDoctorAsync(doctorId);
        return Ok(result);
    }
}