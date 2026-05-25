using ISHMS.Core.Constants;
using Core.DTOs.Auth;
using Core.Interfaces;
using ISHMS.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace DAL.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthRepository(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Email already used."
                };
            }

            var roleExists = await _roleManager.RoleExistsAsync(dto.Role);

            if (!roleExists)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "This role not exis"
                };
            }

            var newUser = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email
            };

            var createResult = await _userManager.CreateAsync(newUser, dto.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ",
                    createResult.Errors.Select(e => e.Description));

                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"Register failed: {errors}"
                };
            }

            await _userManager.AddToRoleAsync(newUser, dto.Role);

            return new AuthResponseDto
            {
                Id = newUser.Id,
                IsAuthenticated = true,
                Message = "Account created Successfuly",
                Email = newUser.Email,
                FullName = newUser.FullName,
                Roles = new List<string> { dto.Role }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Email Doesn't Exist"
                };
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordCorrect)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Password is incorrect"
                };
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new AuthResponseDto
            {
                Id = user.Id,
                IsAuthenticated = true,
                Message = "Correct Data",
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
            };
        }
    }
}