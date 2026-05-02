using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISHMS.Core.Enums;

namespace ISHMS.Core.Models;

public class Alert
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; }

    public string TargetRole { get; set; } = string.Empty;
    public string? TargetUserId { get; set; }              // ✅ nullable
    public ApplicationUser? TargetUser { get; set; }       // ✅ navigation

    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
