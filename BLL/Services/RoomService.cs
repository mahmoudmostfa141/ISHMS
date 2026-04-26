using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class RoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Room> Create(int wardId, string roomNumber)
    {
        var room = new Room
        {
            WardId = wardId,
            RoomNumber = roomNumber
        };

        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();

        return room;
    }

    public async Task<List<Room>> GetByWard(int wardId)
    {
        return await _context.Rooms
            .Where(r => r.WardId == wardId)
            .Include(r => r.Beds)
            .ToListAsync();
    }
}