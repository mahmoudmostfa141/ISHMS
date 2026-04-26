using ISHMS.BLL.Services;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WardController : ControllerBase
{
    private readonly WardService _service;

    public WardController(WardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAll());
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        return Ok(await _service.Create(name));
    }
}