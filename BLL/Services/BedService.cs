using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class BedService
{
    private readonly AppDbContext _context;

    public BedService(AppDbContext context)
    {
        _context = context;
    }

    // ✅ Create Bed
    public async Task<Bed> Create(int roomId)
    {
        var room = await _context.Rooms.FindAsync(roomId);
        if (room == null)
            throw new Exception("Room not found");

        var bed = new Bed
        {
            RoomId = roomId,
            IsOccupied = false
        };

        await _context.Beds.AddAsync(bed);
        await _context.SaveChangesAsync();

        return bed;
    }

    // ✅ Get Beds by Room
    public async Task<List<Bed>> GetByRoom(int roomId)
    {
        return await _context.Beds
            .Where(b => b.RoomId == roomId)
            .Include(b => b.Patient)
            .ToListAsync();
    }

    // 🔥 Assign Patient manually (Admit)
    public async Task AssignPatient(int bedId, int patientId)
    {
        var bed = await _context.Beds.FindAsync(bedId);
        if (bed == null) throw new Exception("Bed not found");

        if (bed.IsOccupied)
            throw new Exception("Bed already occupied");

        // ✅ تأكد إن المريض مش موجود في Waiting List
        var waiting = await _context.WaitingPatients
            .FirstOrDefaultAsync(w => w.PatientId == patientId);

        if (waiting != null)
            _context.WaitingPatients.Remove(waiting);

        bed.IsOccupied = true;
        bed.PatientId = patientId;

        await _context.SaveChangesAsync();
    }

    // 🔥 Discharge + Auto Assign
    public async Task RemovePatient(int bedId)
    {
        var bed = await _context.Beds.FindAsync(bedId);
        if (bed == null) throw new Exception("Bed not found");

        // ✅ فضّي السرير
        bed.IsOccupied = false;
        bed.PatientId = null;

        // 🔥 هات أعلى Priority من Waiting List
        var next = await _context.WaitingPatients
            .Include(w => w.Patient)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.AddedAt)
            .FirstOrDefaultAsync();

        if (next != null)
        {
            // 🔥 Auto Assign
            bed.IsOccupied = true;
            bed.PatientId = next.PatientId;

            // ❌ امسحه من الانتظار
            _context.WaitingPatients.Remove(next);
        }

        await _context.SaveChangesAsync();
    }

    // 🔥 Get Available Beds
    public async Task<List<Bed>> GetAvailableBeds()
    {
        return await _context.Beds
            .Where(b => !b.IsOccupied)
            .ToListAsync();
    }

    // 🔥 Check if Full
    public async Task<bool> IsFull()
    {
        return !await _context.Beds.AnyAsync(b => !b.IsOccupied);
    }
}