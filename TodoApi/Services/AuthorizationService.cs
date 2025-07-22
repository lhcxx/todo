using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly AppDbContext _context;

        public AuthorizationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanAccessTodo(int userId, int todoId)
        {
            var todo = await _context.Todos
                .Include(t => t.Team)
                .FirstOrDefaultAsync(t => t.Id == todoId);

            if (todo == null) return false;

            // User can access their own todos
            if (todo.UserId == userId) return true;

            // User can access shared todos if they're team members
            if (todo.TeamId.HasValue)
            {
                return await IsTeamMember(userId, todo.TeamId.Value);
            }

            return false;
        }

        public async Task<bool> CanModifyTodo(int userId, int todoId)
        {
            var todo = await _context.Todos
                .Include(t => t.Team)
                .FirstOrDefaultAsync(t => t.Id == todoId);

            if (todo == null) return false;

            // User can modify their own todos
            if (todo.UserId == userId) return true;

            // Team admins/owners can modify shared todos
            if (todo.TeamId.HasValue)
            {
                var role = await GetUserTeamRole(userId, todo.TeamId.Value);
                return role == TeamRole.Admin || role == TeamRole.Owner;
            }

            return false;
        }

        public async Task<bool> IsTeamMember(int userId, int teamId)
        {
            return await _context.TeamMembers
                .AnyAsync(tm => tm.UserId == userId && tm.TeamId == teamId);
        }

        public async Task<bool> IsTeamAdmin(int userId, int teamId)
        {
            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);

            return member?.Role == TeamRole.Admin || member?.Role == TeamRole.Owner;
        }

        public async Task<bool> IsTeamOwner(int userId, int teamId)
        {
            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);

            return member?.Role == TeamRole.Owner;
        }

        public async Task<TeamRole> GetUserTeamRole(int userId, int teamId)
        {
            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == teamId);

            return member?.Role ?? TeamRole.Viewer;
        }
    }
} 