using Microsoft.AspNetCore.SignalR;
using BiDE.Models;

namespace BiDE.Hubs
{
    public class InstructorHub : Hub
    {
        // Track connected instructors: ConnectionId -> InstructorId
        private static readonly Dictionary<string, int> ConnectedInstructors = new();

        /// <summary>
        /// Called by the instructor's browser to broadcast their GPS position.
        /// </summary>
        public async Task UpdateLocation(InstructorLocation location)
        {
            ConnectedInstructors[Context.ConnectionId] = location.InstructorId;

            if (location.IsAvailable)
            {
                await Clients.Others.SendAsync("InstructorLocationUpdated", location);
            }
            else
            {
                await Clients.Others.SendAsync("InstructorRemoved", location.InstructorId);
            }
        }

        /// <summary>
        /// Called when instructor clicks "Go Offline".
        /// </summary>
        public async Task GoOffline(int instructorId)
        {
            ConnectedInstructors.Remove(Context.ConnectionId);
            await Clients.Others.SendAsync("InstructorRemoved", instructorId);
        }

        /// <summary>
        /// Called by the server when a booking is made, removes instructor from all maps.
        /// </summary>
        public async Task BroadcastInstructorRemoved(int instructorId)
        {
            await Clients.All.SendAsync("InstructorRemoved", instructorId);
        }

        /// <summary>
        /// Auto-cleanup when instructor closes browser or loses connection.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectedInstructors.TryGetValue(Context.ConnectionId, out int instructorId))
            {
                ConnectedInstructors.Remove(Context.ConnectionId);
                await Clients.Others.SendAsync("InstructorRemoved", instructorId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
