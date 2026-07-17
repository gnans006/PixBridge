using Microsoft.AspNetCore.SignalR;

namespace EventPhoto.Api.Hubs;

/// <summary>
/// SignalR hub for real-time photo and face-search notifications.
///
/// Group naming:
///   event-{eventId}          → broadcast new photos to all gallery viewers
///   face-session-{token}     → private channel per guest face-search session
/// </summary>
public sealed class PhotoHub : Hub
{
    /// <summary>
    /// Joins the caller to the event group (photo updates) and,
    /// when a <c>sessionToken</c> query param is supplied, to their
    /// private face-search session group.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var httpCtx = Context.GetHttpContext();

        var eventId = httpCtx?.Request.Query["eventId"].ToString();
        if (!string.IsNullOrWhiteSpace(eventId))
            await Groups.AddToGroupAsync(Context.ConnectionId, GetEventGroupName(eventId));

        var sessionToken = httpCtx?.Request.Query["sessionToken"].ToString();
        if (!string.IsNullOrWhiteSpace(sessionToken))
            await Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionToken));

        await base.OnConnectedAsync();
    }

    /// <summary>Adds the caller to an event broadcast group.</summary>
    public Task JoinEvent(string eventId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GetEventGroupName(eventId));

    /// <summary>Removes the caller from an event broadcast group.</summary>
    public Task LeaveEvent(string eventId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetEventGroupName(eventId));

    /// <summary>Adds the caller to their private face-search session group.</summary>
    public Task JoinFaceSession(string sessionToken) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionToken));

    /// <summary>Removes the caller from their face-search session group.</summary>
    public Task LeaveFaceSession(string sessionToken) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSessionGroupName(sessionToken));

    /// <summary>Builds the SignalR group name for a photo-gallery event.</summary>
    public static string GetEventGroupName(string eventId) => $"event-{eventId}";

    /// <summary>Builds the SignalR group name for a guest face-search session.</summary>
    public static string GetSessionGroupName(string sessionToken) => $"face-session-{sessionToken}";
}
