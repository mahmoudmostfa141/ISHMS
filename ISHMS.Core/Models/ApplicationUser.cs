using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ISHMS.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        // IdentityUser :
        // Id, UserName, Email, PasswordHash, PhoneNumber ...

        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
