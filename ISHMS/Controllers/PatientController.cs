using ISHMS.Core.DTOs;
using ISHMS.Core.Interfaces;
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

    // ✅ Create Patient
    [HttpPost]
    public async Task<IActionResult> Create(CreatePatientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.Create(dto);
        return Ok(result);
    }

    // ✅ Get All Patients
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAll();
        return Ok(result);
    }

    // ✅ Get Patient By Id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetById(id);

        if (result == null)
            return NotFound("Patient not found");

        return Ok(result);
    }

    // ✅ Update Patient
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdatePatientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _service.Update(id, dto);
        return Ok("Updated Successfully");
    }

    // ✅ Delete Patient
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.Delete(id);
        return Ok("Deleted Successfully");
    }

    // ✅ Add Vital Signs
    [HttpPost("vital")]
    public async Task<IActionResult> AddVital(CreateVitalDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _service.AddVital(dto);
        return Ok("Vital Added");
    }
}