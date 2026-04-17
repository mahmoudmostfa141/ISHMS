using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;

    public PatientController(IPatientService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(Patient patient)
    {
        var result = await _service.CreatePatientAsync(patient);
        return Ok(result);
    }

    [HttpPost("vitals")]
    public async Task<IActionResult> AddVital(VitalSign vital)
    {
        var result = await _service.AddVitalSignAsync(vital);
        return Ok(result);
    }
}