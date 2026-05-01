using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentController : ControllerBase
{
    private readonly IDepartmentService _service;

    public DepartmentController(IDepartmentService service)
    {
        _service = service;
    }

    // 🔥 GET ALL Departments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAll();
        return Ok(result);
    }
}