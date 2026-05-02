using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Task;
using ISHMS.Core.Enums;
using ISHMS.Core.Interfaces;
using ISHMS.Core.Models;
using ISHMS.DAL;
using Microsoft.EntityFrameworkCore;

namespace ISHMS.BLL.Services;

public class PatientTaskService : IPatientTaskService
{
    private readonly AppDbContext _context;

    public PatientTaskService(AppDbContext context)
    {
        _context = context;
    }

    // ==================== Create ====================

    public async Task CreateAsync(CreatePatientTaskDto dto)
    {
        var task = new PatientTask
        {
            PatientId = dto.PatientId,
            AssignedToRole = dto.AssignedToRole,
            AssignedToUserId = dto.AssignedToUserId,  // nullable
            Title = dto.Title,
            Description = dto.Description,
            Status = PatientTaskStatus.Pending
        };

        await _context.PatientTasks.AddAsync(task);
        await _context.SaveChangesAsync();
    }

    // ==================== Get By Patient ====================

    public async Task<List<PatientTaskResponseDto>> GetByPatientAsync(int patientId)
    {
        return await _context.PatientTasks
            .Include(t => t.Patient)
            .Include(t => t.AssignedToUser)
            .Where(t => t.PatientId == patientId)
            .Select(t => ToDto(t))
            .ToListAsync();
    }

    // ==================== Get By Role ====================

    public async Task<List<PatientTaskResponseDto>> GetByRoleAsync(string role)
    {
        return await _context.PatientTasks
            .Include(t => t.Patient)
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedToRole == role
                     && t.Status == PatientTaskStatus.Pending)
            .Select(t => ToDto(t))
            .ToListAsync();
    }

    // ==================== Get By User ====================

    public async Task<List<PatientTaskResponseDto>> GetByUserAsync(string userId)
    {
        return await _context.PatientTasks
            .Include(t => t.Patient)
            .Include(t => t.AssignedToUser)
            .Where(t => t.AssignedToUserId == userId
                     && t.Status == PatientTaskStatus.Pending)
            .Select(t => ToDto(t))
            .ToListAsync();
    }

    // ==================== Complete ====================

    public async Task CompleteAsync(int taskId)
    {
        var task = await _context.PatientTasks.FindAsync(taskId);

        if (task == null)
            throw new Exception("Task not found");

        if (task.Status == PatientTaskStatus.Completed)
            throw new Exception("Task already completed");

        task.Status = PatientTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // ==================== Mapper ====================

    private static PatientTaskResponseDto ToDto(PatientTask t)
    {
        return new PatientTaskResponseDto
        {
            Id = t.Id,
            PatientId = t.PatientId,
            PatientName = t.Patient.FullName,
            AssignedToRole = t.AssignedToRole,
            AssignedToUserName = t.AssignedToUser != null
                ? t.AssignedToUser.FullName
                : null,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),
            CreatedAt = t.CreatedAt
        };
    }
}