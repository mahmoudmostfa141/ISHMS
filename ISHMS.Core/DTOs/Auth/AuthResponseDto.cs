namespace Core.DTOs.Auth       
{
    public class AuthResponseDto
    {
        public bool IsAuthenticated { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}