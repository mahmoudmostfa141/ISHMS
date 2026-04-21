using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Auth        
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full Name Requred")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Required")]
        [EmailAddress(ErrorMessage = "Wrong Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password Requred")]
        [MinLength(6, ErrorMessage = "Password Must Have At least 6 digits")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role Requird")]
        public string Role { get; set; } = string.Empty;
    }
}