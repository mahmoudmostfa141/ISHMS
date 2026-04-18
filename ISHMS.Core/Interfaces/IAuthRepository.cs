using Core.DTOs.Auth;

namespace Core.Interfaces
{
    public interface IAuthRepository
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
     

        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        
    }
}