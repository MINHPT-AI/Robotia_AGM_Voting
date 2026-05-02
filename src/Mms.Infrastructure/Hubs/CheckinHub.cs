using Microsoft.AspNetCore.SignalR;

namespace Mms.Infrastructure.Hubs;

/// <summary>
/// SignalR Hub cho real-time check-in updates (BRD v2.3 Mục 5).
/// Tất cả POS terminals subscribe vào meeting group.
/// </summary>
public class CheckinHub : Hub
{
    /// <summary>POS terminal tham gia vào group của meeting.</summary>
    public async Task JoinMeeting(Guid meetingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"meeting-{meetingId}");
    }

    /// <summary>POS terminal rời group.</summary>
    public async Task LeaveMeeting(Guid meetingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"meeting-{meetingId}");
    }
}

/// <summary>Helper methods để broadcast từ server-side code.</summary>
public static class CheckinHubExtensions
{
    /// <summary>Broadcast cập nhật Topbar 3 dòng cho tất cả POS.</summary>
    public static async Task BroadcastTopbarUpdate(
        this IHubContext<CheckinHub> hub, Guid meetingId, object topbarData)
    {
        await hub.Clients.Group($"meeting-{meetingId}")
            .SendAsync("TopbarUpdated", topbarData);
    }

    /// <summary>Broadcast cảnh báo phiếu bị hủy / cần in lại.</summary>
    public static async Task BroadcastBallotLifecycleAlert(
        this IHubContext<CheckinHub> hub, Guid meetingId, string alertType, object alertData)
    {
        await hub.Clients.Group($"meeting-{meetingId}")
            .SendAsync("BallotLifecycleAlert", alertType, alertData);
    }

    /// <summary>Broadcast khi đạt đủ tỷ lệ điều kiện họp.</summary>
    public static async Task BroadcastQuorumReached(
        this IHubContext<CheckinHub> hub, Guid meetingId, decimal percentage)
    {
        await hub.Clients.Group($"meeting-{meetingId}")
            .SendAsync("QuorumReached", percentage);
    }
}
