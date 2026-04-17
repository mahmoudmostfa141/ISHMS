using System.ComponentModel.DataAnnotations;
using ISHMS.Core.Enums;

namespace ISHMS.Core.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100)]
    public string FullName { get; set; }

    [Range(0, 120)]
    public int Age { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public PatientStatus CurrentStatus { get; set; } = PatientStatus.Stable;

    public DateTime AdmittedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VitalSign>? VitalSigns { get; set; }
}