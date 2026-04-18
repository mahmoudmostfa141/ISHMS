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

        // ==================== Register ====================

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Check Email not exist
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            // If find user with same email
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "Email already used."
                };
            }

            //Check if role exis in db
            var roleExists = await _roleManager.RoleExistsAsync(dto.Role);

            if (!roleExists)
            {
                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = "This role not exis"
                };
            }

            // Create new user
            var newUser = new ApplicationUser
            {
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.Email,
                // UserName = Email because Identity needs UserName
                // Use Email as Username
            };

            // Save user to db and make Hash to Password
            var createResult = await _userManager.CreateAsync(newUser, dto.Password);
            // CreateAsync makes Hash to Password automatic 
            // Saves User in AspNetUsers table

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ",
                    createResult.Errors.Select(e => e.Description));
                // Collect all Errors in single String 

                return new AuthResponseDto
                {
                    IsAuthenticated = false,
                    Message = $"Register failed: {errors}"
                };
            }

            // Add role to user
            await _userManager.AddToRoleAsync(newUser, dto.Role);
            // Saves in AspNetUserRoles table

            return new AuthResponseDto
            {
                IsAuthenticated = true,
                Message = "Account created Successfuly",
                Email = newUser.Email,
                FullName = newUser.FullName,
                Roles = new List<string> { dto.Role }
                
            };
        }

        // ==================== Login ====================

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

            var isPasswordCorrect = await _userManager
                                        .CheckPasswordAsync(user, dto.Password);
            // CheckPasswordAsync makes Hash to input Password 
            // then compare it to Hash saved in DB ✅

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
                IsAuthenticated = true,
                Message = "Correct Data",
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles
               
            };
        }
    }
}