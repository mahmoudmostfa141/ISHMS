using Core.DTOs.Auth;

namespace Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    

        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    }
}