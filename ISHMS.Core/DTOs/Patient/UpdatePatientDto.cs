using System.ComponentModel.DataAnnotations;

namespace ISHMS.Core.DTOs;

public class UpdatePatientDto
{
    [Required]
    public string FullName { get; set; }

    public int Age { get; set; }

    public DateTime DateOfBirth { get; set; }
}