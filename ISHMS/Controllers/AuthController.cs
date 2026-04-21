using Core.DTOs.Auth;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ISHMS.API.Controllers
{
    [Route("api/[controller]")]
    //Auth

    [ApiController]

    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // ==================== Register ====================

        [HttpPost("register")]
        //POST api/Auth/register
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(dto);

            if (!result.IsAuthenticated)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        // ==================== Login ====================

        [HttpPost("login")]
        //POST api/Auth/login
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(dto);

            if (!result.IsAuthenticated)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
    }
}