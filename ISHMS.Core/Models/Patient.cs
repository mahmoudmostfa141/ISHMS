using System.ComponentModel.DataAnnotations;
using ISHMS.Core.Enums;

namespace ISHMS.Core.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; }

    public int Age { get; set; }

    public DateTime DateOfBirth { get; set; }

    public PatientStatus CurrentStatus { get; set; }

    public PriorityLevel Priority { get; set; }

    public int NewsScore { get; set; }
    public ICollection<VitalSign>? VitalSigns { get; set; }
    public DateTime AdmittedAt { get; set; }
}