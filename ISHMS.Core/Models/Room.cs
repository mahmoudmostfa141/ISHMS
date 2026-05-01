namespace ISHMS.Core.Models;

public class Room
{
    public int Id { get; set; }

    public string RoomNumber { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; }

    public List<Bed> Beds { get; set; } = new();
}