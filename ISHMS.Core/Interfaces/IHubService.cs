namespace ISHMS.Core.Interfaces;

public interface IHubService
{
    Task SendStatusUpdateAsync(int patientId, string patientName, string newStatus);

    Task SendNewsScoreUpdateAsync(int patientId, string patientName, int newsScore, string status);

    Task SendAlertAsync(int patientId, string patientName, string targetRole, string? targetUserId, string message, string severity);

    Task SendTaskAsync(int patientId, string patientName, string assignedToRole, string? assignedToUserId, string title, string description);
    Task SendMedicalReportAsync(int patientId, string patientName, string doctorName, string reportType);

}