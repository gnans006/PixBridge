using Microsoft.AspNetCore.SignalR;

namespace EventPhoto.Api.Hubs;

/// <summary>
/// SignalR hub for real-time photo notifications to gallery clients.
/// Clients join groups named after event identifiers to receive targeted updates.
/// </summary>
public sealed class PhotoHub : Hub
{
    /// <summary>
    /// Joins the caller to the event group specified by the <c>eventId</c> query parameter when present.
    /// </summary>
    /// <returns>A task that completes when connection initialization finishes.</returns>
    public override async Task OnConnectedAsync()
    {
        var eventId = Context.GetHttpContext()?.Request.Query["eventId"].ToString();
        if (!string.IsNullOrWhiteSpace(eventId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetEventGroupName(eventId));
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Adds the current connection to the specified event group.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>A task that completes when the group membership is updated.</returns>
    public Task JoinEvent(string eventId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GetEventGroupName(eventId));

    /// <summary>
    /// Removes the current connection from the specified event group.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>A task that completes when the group membership is updated.</returns>
    public Task LeaveEvent(string eventId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetEventGroupName(eventId));

    /// <summary>
    /// Builds the SignalR group name for an event.
    /// </summary>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>The normalized group name.</returns>
    public static string GetEventGroupName(string eventId) => $"event-{eventId}";
}
