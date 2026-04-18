using ISHMS.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // [Authorize] على الـ Controller كلها
    // يعني كل الـ Endpoints محتاجة Token صحيح
    public class TestController : ControllerBase
    {
        [HttpGet("admin-only")]
        [Authorize(Roles = AppRoles.Admin)]
        // بس الـ Admin يقدر يوصل
        public IActionResult AdminOnly()
        {
            return Ok(new { Message = "أهلاً Admin!" });
        }

        [HttpGet("doctor-only")]
        [Authorize(Roles = AppRoles.Doctor)]
        public IActionResult DoctorOnly()
        {
            return Ok(new { Message = "أهلاً Doctor!" });
        }

        [HttpGet("nurse-only")]
        [Authorize(Roles = AppRoles.Nurse)]
        public IActionResult NurseOnly()
        {
            return Ok(new { Message = "أهلاً Nurse!" });
        }

        [HttpGet("multi-roles")]
        [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Doctor}")]
        // Admin أو Doctor يقدر يوصل
        public IActionResult AdminOrDoctor()
        {
            return Ok(new { Message = "أهلاً Admin أو Doctor!" });
        }
    }
}