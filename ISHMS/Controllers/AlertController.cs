using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    // GET api/alert/role/Doctor
    [HttpGet("role/{role}")]
    public async Task<IActionResult> GetByRole(string role)
    {
        var result = await _alertService.GetByRoleAsync(role);
        return Ok(result);
    }

    // GET api/alert/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var result = await _alertService.GetByUserAsync(userId);
        return Ok(result);
    }

    // POST api/alert/read/{alertId}
    [HttpPost("read/{alertId}")]
    public async Task<IActionResult> MarkAsRead(int alertId)
    {
        await _alertService.MarkAsReadAsync(alertId);
        return Ok("Alert marked as read");
    }
}