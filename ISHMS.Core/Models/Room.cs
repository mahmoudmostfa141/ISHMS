namespace ISHMS.Core.Models;

public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;

    public int WardId { get; set; }
    public Ward Ward { get; set; }

    public ICollection<Bed> Beds { get; set; } = new List<Bed>();
}