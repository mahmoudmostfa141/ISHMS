using Microsoft.AspNetCore.SignalR;
using ISHMS.Core.Interfaces;
using ISHMS.API.Hubs;

namespace ISHMS.API.Realtime;

public class HubService : IHubService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public HubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // لما الـ FlowStatus يتغير
    public async Task SendStatusUpdateAsync(
        int patientId,
        string patientName,
        string newStatus)
    {
        await _hubContext.Clients.All.SendAsync(
            "ReceiveStatusUpdate",
            new { patientId, patientName, newStatus, timestamp = DateTime.UtcNow }
        );
    }

    // لما NEWS Score يتحدث
    public async Task SendNewsScoreUpdateAsync(
        int patientId,
        string patientName,
        int newsScore,
        string status)
    {
        await _hubContext.Clients.All.SendAsync(
            "ReceiveNewsUpdate",
            new { patientId, patientName, newsScore, status, timestamp = DateTime.UtcNow }
        );
    }

    // لما Alert جديد يتولد
    public async Task SendAlertAsync(
        int patientId,
        string patientName,
        string targetRole,
        string? targetUserId,
        string message,
        string severity)
    {
        Console.WriteLine("SignalR Alert Fired");

        // Alert → الـ Role المحددة بس
        await _hubContext.Clients
            .Group(targetRole)
            .SendAsync(
                "ReceiveAlert",
                new { patientId, patientName, targetRole, targetUserId, message, severity, timestamp = DateTime.UtcNow }
            );
    }

    public async Task SendTaskAsync(
        int patientId,
        string patientName,
        string assignedToRole,
        string? assignedToUserId,
        string title,
        string description)
    {
        // Task → الـ Role المحددة بس
        await _hubContext.Clients
            .Group(assignedToRole)
            .SendAsync(
                "ReceiveTask",
                new { patientId, patientName, assignedToRole, assignedToUserId, title, description, timestamp = DateTime.UtcNow }
            );

    }
    public async Task SendMedicalReportAsync(
    int patientId,
    string patientName,
    string doctorName,
    string reportType)
    {
        await _hubContext.Clients.All.SendAsync(
            "ReceiveMedicalReport",
            new { patientId, patientName, doctorName, reportType, timestamp = DateTime.UtcNow }
        );
    }

}