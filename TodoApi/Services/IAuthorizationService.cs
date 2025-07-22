using TodoApi.Models;

namespace TodoApi.Services
{
    public interface IAuthorizationService
    {
        Task<bool> CanAccessTodo(int userId, int todoId);
        Task<bool> CanModifyTodo(int userId, int todoId);
        Task<bool> IsTeamMember(int userId, int teamId);
        Task<bool> IsTeamAdmin(int userId, int teamId);
        Task<bool> IsTeamOwner(int userId, int teamId);
        Task<TeamRole> GetUserTeamRole(int userId, int teamId);
    }
} 