public class CreatePatientDto
{
    public string FullName { get; set; }
    public int Age { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int DepartmentId { get; set; }  // ✅ القسم
    public int BedId { get; set; }         // ✅ السرير (مش nullable)
}