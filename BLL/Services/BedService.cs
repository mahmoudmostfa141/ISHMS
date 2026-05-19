using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.DepartmentBed;
using ISHMS.Core.Interfaces;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class BedService : IBedService
{
    private readonly AppDbContext _context;

    public BedService(AppDbContext context)
    {
        _context = context;
    }

    public async Task AssignPatient(AssignBedDto dto)
    {
        // 🔥 جيب أول سرير فاضي في القسم
        var bed = await _context.Beds
            .Include(b => b.Room)
            .Where(b => !b.IsOccupied &&
                        b.Room.DepartmentId == dto.DepartmentId)
            .FirstOrDefaultAsync();

        if (bed == null)
            throw new Exception("No beds available in this department");

        // 🔥 اربط المريض
        bed.IsOccupied = true;
        bed.PatientId = dto.PatientId;

        var patient = await _context.Patients.FindAsync(dto.PatientId);
        if (patient == null)
            throw new Exception("Patient not found");

        patient.BedId = bed.Id;

        await _context.SaveChangesAsync();
    }

    // كل الأسرّة المتاحة
    public async Task<List<AvailableBedDto>> GetAvailableBeds()
    {
        return await _context.Beds
            .Where(b => !b.IsOccupied)
            .Include(b => b.Room)
                .ThenInclude(r => r.Department)
            .Select(b => new AvailableBedDto
            {
                BedId = b.Id,
                RoomNumber = b.Room.RoomNumber,
                DepartmentId = b.Room.Department.Id,
                DepartmentName = b.Room.Department.Name
            })
            .ToListAsync();
    }

    // الأسرّة المتاحة في قسم معين
    public async Task<List<AvailableBedDto>> GetAvailableBedsByDepartment(int departmentId)
    {
        return await _context.Beds
            .Where(b => !b.IsOccupied && b.Room.Department.Id == departmentId)
            .Include(b => b.Room)
                .ThenInclude(r => r.Department)
            .Select(b => new AvailableBedDto
            {
                BedId = b.Id,
                RoomNumber = b.Room.RoomNumber,
                DepartmentId = b.Room.Department.Id,
                DepartmentName = b.Room.Department.Name
            })
            .ToListAsync();
    }
    // كل الأسرّة المشغولة مع بيانات المريض
    public async Task<List<OccupiedBedDto>> GetOccupiedBeds()
    {
        return await _context.Beds
            .Include(b => b.Room)
                .ThenInclude(r => r.Department)
            .Include(b => b.Patient)
            .Where(b => b.IsOccupied && b.Patient != null)
            .Select(b => new OccupiedBedDto
            {
                BedId = b.Id,
                RoomNumber = b.Room.RoomNumber,
                DepartmentName = b.Room.Department.Name,
                PatientId = b.Patient!.Id,
                PatientName = b.Patient.FullName,
                Age = b.Patient.Age,
                FlowStatus = b.Patient.FlowStatus.ToString(),
                NewsScore = b.Patient.NewsScore,
                AdmittedAt = b.Patient.AdmittedAt
            })
            .ToListAsync();
    }
}