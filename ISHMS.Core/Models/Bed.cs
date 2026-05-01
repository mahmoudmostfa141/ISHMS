namespace ISHMS.Core.Models;

public class Bed
{
    public int Id { get; set; }

    public bool IsOccupied { get; set; } = false;

    public int RoomId { get; set; }
    public Room Room { get; set; }

    public int? PatientId { get; set; }
    public Patient? Patient { get; set; }
}