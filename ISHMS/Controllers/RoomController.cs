using ISHMS.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController : ControllerBase
{
    private readonly RoomService _service;

    public RoomController(RoomService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(int wardId, string roomNumber)
    {
        return Ok(await _service.Create(wardId, roomNumber));
    }

    [HttpGet("{wardId}")]
    public async Task<IActionResult> GetByWard(int wardId)
    {
        return Ok(await _service.GetByWard(wardId));
    }
}