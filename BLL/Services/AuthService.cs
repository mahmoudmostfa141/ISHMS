using Core.DTOs.Auth;
using Core.Interfaces;
using Core.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            IAuthRepository authRepository,
            IOptions<JwtSettings> jwtSettings)
        {
            _authRepository = authRepository;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var result = await _authRepository.RegisterAsync(dto);

            if (!result.IsAuthenticated)
            {
                return result;
            }

            var token = GenerateJwtToken(
                result.Id!,
                result.Email!,
                result.FullName!,
                result.Roles);

            result.Token = token;
            result.TokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.DurationInDays);

            return result;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var result = await _authRepository.LoginAsync(dto);

            if (!result.IsAuthenticated)
            {
                return result;
            }

            var token = GenerateJwtToken(
                result.Id!,
                result.Email!,
                result.FullName!,
                result.Roles);

            result.Token = token;
            result.TokenExpiration = DateTime.UtcNow.AddDays(_jwtSettings.DurationInDays);

            return result;
        }

        private string GenerateJwtToken(
            string userId,
            string email,
            string fullName,
            IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            var symmetricKey = new SymmetricSecurityKey(keyBytes);

            var signingCredentials = new SigningCredentials(
                symmetricKey,
                SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwtSettings.DurationInDays),
                signingCredentials: signingCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}