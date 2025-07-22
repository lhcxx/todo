using Microsoft.AspNetCore.SignalR;
using TodoApi.DTOs;

namespace TodoApi.Hubs
{
    public class TodoHub : Hub
    {
        public async Task JoinTeam(string teamId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"team_{teamId}");
        }

        public async Task LeaveTeam(string teamId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"team_{teamId}");
        }

        public async Task TodoUpdated(int teamId, TodoReadDto todo)
        {
            await Clients.Group($"team_{teamId}").SendAsync("TodoUpdated", todo);
        }

        public async Task TodoCreated(int teamId, TodoReadDto todo)
        {
            await Clients.Group($"team_{teamId}").SendAsync("TodoCreated", todo);
        }

        public async Task TodoDeleted(int teamId, int todoId)
        {
            await Clients.Group($"team_{teamId}").SendAsync("TodoDeleted", todoId);
        }

        public async Task ActivityAdded(int teamId, ActivityReadDto activity)
        {
            await Clients.Group($"team_{teamId}").SendAsync("ActivityAdded", activity);
        }

        public async Task MemberJoined(int teamId, TeamMemberDto member)
        {
            await Clients.Group($"team_{teamId}").SendAsync("MemberJoined", member);
        }

        public async Task MemberLeft(int teamId, int userId)
        {
            await Clients.Group($"team_{teamId}").SendAsync("MemberLeft", userId);
        }
    }
} 