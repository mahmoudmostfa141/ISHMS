using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISHMS.Core.DTOs.Task
{
    public class CreatePatientTaskDto
    {
        public int PatientId { get; set; }
        public string AssignedToRole { get; set; } = string.Empty;
        public string? AssignedToUserId { get; set; }   // ✅ nullable — Role-based أو User-based
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
