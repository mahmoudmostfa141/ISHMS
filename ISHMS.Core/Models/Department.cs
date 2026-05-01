namespace ISHMS.Core.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; }

    public List<Room> Rooms { get; set; } = new();
}