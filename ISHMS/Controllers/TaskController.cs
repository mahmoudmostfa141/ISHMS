using ISHMS.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskController : ControllerBase
{
    private readonly IPatientTaskService _taskService;

    public TaskController(IPatientTaskService taskService)
    {
        _taskService = taskService;
    }

    // GET api/task/role/Nurse
    [HttpGet("role/{role}")]
    public async Task<IActionResult> GetByRole(string role)
    {
        var result = await _taskService.GetByRoleAsync(role);
        return Ok(result);
    }

    // GET api/task/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        var result = await _taskService.GetByUserAsync(userId);
        return Ok(result);
    }

    // GET api/task/patient/{patientId}
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var result = await _taskService.GetByPatientAsync(patientId);
        return Ok(result);
    }

    // POST api/task/complete/{taskId}
    [HttpPost("complete/{taskId}")]
    public async Task<IActionResult> Complete(int taskId)
    {
        await _taskService.CompleteAsync(taskId);
        return Ok("Task completed successfully");
    }
}