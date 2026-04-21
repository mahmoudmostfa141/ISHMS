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

        // ==================== Register ====================

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var result = await _authRepository.RegisterAsync(dto);

            if (!result.IsAuthenticated)
            {
                return result;
            }

            // Generate token
            var token = GenerateJwtToken(
                result.Email!,
                result.FullName!,
                result.Roles);

            // add token to response
            result.Token = token;
            result.TokenExpiration = DateTime
                .UtcNow
                .AddDays(_jwtSettings.DurationInDays);

            return result;
        }

        // ==================== Login ====================

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var result = await _authRepository.LoginAsync(dto);

            if (!result.IsAuthenticated)
            {
                return result;
            }
            // Generate token
            var token = GenerateJwtToken(
                result.Email!,
                result.FullName!,
                result.Roles);
            // add token to response
            result.Token = token;
            result.TokenExpiration = DateTime
                .UtcNow
                .AddDays(_jwtSettings.DurationInDays);

            return result;
        }

        // ==================== Generate JWT Token ====================

        private string GenerateJwtToken(
            string email,
            string fullName,
            IList<string> roles)
        {
            // Create Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),

                new Claim(ClaimTypes.Name, fullName),

                new Claim(JwtRegisteredClaimNames.Jti,
                          Guid.NewGuid().ToString()),
                // Jti = JWT ID 
            };

            // Add Roles as Claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
                
            }

            // Create Signing Key
            var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            // Convert Key from String to Bytes

            var symmetricKey = new SymmetricSecurityKey(keyBytes);
            // Create Security Key from Bytes

            var signingCredentials = new SigningCredentials(
                symmetricKey,
                SecurityAlgorithms.HmacSha256);
            // Select encoding Algorithm 

            // Building Token
            var tokenDescriptor = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,

                audience: _jwtSettings.Audience,

                claims: claims,

                expires: DateTime.UtcNow
                                 .AddDays(_jwtSettings.DurationInDays),

                signingCredentials: signingCredentials
            );

            // Convert Token to String 
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
            // WriteToken Convert Token Object To String
            // eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        }
    }
}