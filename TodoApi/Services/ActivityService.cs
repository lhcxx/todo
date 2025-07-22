using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services
{
    public interface IActivityService
    {
        Task LogActivityAsync(int userId, ActivityType type, string description, int? teamId = null, int? todoId = null);
    }

    public class ActivityService : IActivityService
    {
        private readonly AppDbContext _context;

        public ActivityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(int userId, ActivityType type, string description, int? teamId = null, int? todoId = null)
        {
            var activity = new Activity
            {
                Type = type,
                Description = description,
                UserId = userId,
                TeamId = teamId,
                TodoId = todoId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
        }
    }
} 