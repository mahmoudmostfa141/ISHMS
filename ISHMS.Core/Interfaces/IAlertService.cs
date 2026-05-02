using ISHMS.Core.DTOs;
using ISHMS.Core.DTOs.Alert;

namespace ISHMS.Core.Interfaces;

public interface IAlertService
{
    Task CreateAsync(CreateAlertDto dto);
    Task<List<AlertResponseDto>> GetByRoleAsync(string role);
    Task<List<AlertResponseDto>> GetByUserAsync(string userId);  // ✅ User-based
    Task MarkAsReadAsync(int alertId);
}