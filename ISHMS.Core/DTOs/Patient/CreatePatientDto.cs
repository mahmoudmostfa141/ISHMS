using System.ComponentModel.DataAnnotations;

namespace ISHMS.Core.DTOs;

public class CreatePatientDto
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; }

    [Range(0, 120)]
    public int Age { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }
}