using ISHMS.Core.Constants;
using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Alert;
using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;


public class AlertService : IAlertService
{
    private readonly AppDbContext _context;
    private readonly IHubService _hubService;


    public AlertService(AppDbContext context, IHubService hubService)
    {
        _context = context;
        _hubService = hubService;
    }

    // ==================== Create ====================

    public async Task CreateAsync(CreateAlertDto dto)
    {
        var alert = new Alert
        {
            PatientId = dto.PatientId,
            TargetRole = dto.TargetRole,
            TargetUserId = dto.TargetUserId,  // nullable
            Message = dto.Message,
            Severity = dto.Severity,
            IsRead = false
        };
        var patientName = await _context.Alerts.Where(a => a.PatientId == dto.PatientId).Select(a => a.Patient.FullName).FirstOrDefaultAsync();
        await _hubService.SendAlertAsync(
                   dto.PatientId,
                   patientName,
                   dto.TargetRole,
                   dto.TargetUserId,
                   dto.Message,
                   dto.Severity.ToString()
               );

        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();
    }

    // ==================== Get By Role ====================

    public async Task<List<AlertResponseDto>> GetByRoleAsync(string role)
    {
        return await _context.Alerts
            .Include(a => a.Patient)
            .Include(a => a.TargetUser)
            .Where(a => a.TargetRole == role && !a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    // ==================== Get By User ====================

    public async Task<List<AlertResponseDto>> GetByUserAsync(string userId)
    {
        return await _context.Alerts
            .Include(a => a.Patient)
            .Include(a => a.TargetUser)
            .Where(a => a.TargetUserId == userId && !a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    // ==================== Mark As Read ====================

    public async Task MarkAsReadAsync(int alertId)
    {
        var alert = await _context.Alerts.FindAsync(alertId);

        if (alert == null)
            throw new Exception("Alert not found");

        alert.IsRead = true;
        await _context.SaveChangesAsync();
    }

    // ==================== Mapper ====================

    private static AlertResponseDto ToDto(Alert a)
    {
        return new AlertResponseDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = a.Patient.FullName,
            TargetRole = a.TargetRole,
            TargetUserName = a.TargetUser != null
                ? a.TargetUser.FullName
                : null,
            Message = a.Message,
            Severity = a.Severity.ToString(),
            IsRead = a.IsRead,
            CreatedAt = a.CreatedAt
        };
    }
}