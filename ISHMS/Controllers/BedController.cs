using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.DepartmentBed;
using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BedController : ControllerBase
{
    private readonly IBedService _service;

    public BedController(IBedService service)
    {
        _service = service;
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign(AssignBedDto dto)
    {
        await _service.AssignPatient(dto);
        return Ok("Patient Assigned Successfully");
    }

    // ✅ كل الأسرّة المتاحة
    [HttpGet("available")]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> GetAvailableBeds()
    {
        var beds = await _service.GetAvailableBeds();
        return Ok(beds);
    }

    // ✅ أسرّة متاحة في قسم معين
    [HttpGet("available/{departmentId}")]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> GetAvailableBedsByDepartment(int departmentId)
    {
        var beds = await _service.GetAvailableBedsByDepartment(departmentId);
        return Ok(beds);
    }

    // ✅ الأسرّة المشغولة مع بيانات المرضى
    [HttpGet("occupied")]
    [Authorize(Roles = "Receptionist,Admin,Doctor,Nurse")]
    public async Task<IActionResult> GetOccupiedBeds()
    {
        var beds = await _service.GetOccupiedBeds();
        return Ok(beds);
    }
}
