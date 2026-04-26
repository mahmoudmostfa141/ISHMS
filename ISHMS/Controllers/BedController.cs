using ISHMS.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BedController : ControllerBase
{
    private readonly BedService _service;

    public BedController(BedService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(int roomId)
    {
        return Ok(await _service.Create(roomId));
    }

    [HttpGet("{roomId}")]
    public async Task<IActionResult> GetByRoom(int roomId)
    {
        return Ok(await _service.GetByRoom(roomId));
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign(int bedId, int patientId)
    {
        await _service.AssignPatient(bedId, patientId);
        return Ok("Patient Assigned");
    }

    [HttpPost("discharge")]
    public async Task<IActionResult> Discharge(int bedId)
    {
        await _service.RemovePatient(bedId);
        return Ok("Patient Discharged");
    }
}
