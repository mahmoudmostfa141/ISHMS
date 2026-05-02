using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Alert
{
    public class AlertResponseDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string? TargetUserName { get; set; }  
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
