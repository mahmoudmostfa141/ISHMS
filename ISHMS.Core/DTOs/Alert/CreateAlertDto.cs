using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.Enums;

namespace ISHMS.Core.DTOs.Alert
{
    public class CreateAlertDto
    {
        public int PatientId { get; set; }
        public string TargetRole { get; set; } = string.Empty;
        public string? TargetUserId { get; set; }   
        public string Message { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
    }

}
