using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ISHMS.Core.Constants;
using System.Security.Claims;

namespace ISHMS.API.Hubs;

[Authorize]  // محتاج Token صحيح عشان يتوصل
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // بنقرأ الـ Role من الـ JWT Token مباشرة
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);

        if (!string.IsNullOrEmpty(role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, role);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR بيشيل الـ Client من الـ Groups تلقائياً عند Disconnect
        await base.OnDisconnectedAsync(exception);
    }
}