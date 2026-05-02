using ISHMS.Core.Constants;
using ISHMS.Core.Enums;

namespace ISHMS.Core.Models;

public class PatientTask
{
   
        public int Id { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public string AssignedToRole { get; set; } = string.Empty;
        public string? AssignedToUserId { get; set; }          // ✅ nullable
        public ApplicationUser? AssignedToUser { get; set; }   // ✅ navigation

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public PatientTaskStatus Status { get; set; } = PatientTaskStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    
}