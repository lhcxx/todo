using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TodoApi.Services.IAuthorizationService _authService;

        public ActivityController(AppDbContext context, TodoApi.Services.IAuthorizationService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActivityReadDto>>> GetActivities([FromQuery] ActivityFilterDto filter)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var query = _context.Activities
                .Include(a => a.User)
                .Include(a => a.Team)
                .Include(a => a.Todo)
                .AsQueryable();

            // Filter by team if specified
            if (filter.TeamId.HasValue)
            {
                if (!await _authService.IsTeamMember(userId, filter.TeamId.Value))
                    return Forbid();
                query = query.Where(a => a.TeamId == filter.TeamId);
            }
            else
            {
                // Show user's own activities and team activities they're part of
                var userTeams = await _context.TeamMembers
                    .Where(tm => tm.UserId == userId)
                    .Select(tm => tm.TeamId)
                    .ToListAsync();
                
                query = query.Where(a => a.UserId == userId || (a.TeamId.HasValue && userTeams.Contains(a.TeamId.Value)));
            }

            // Apply other filters
            if (filter.UserId.HasValue)
                query = query.Where(a => a.UserId == filter.UserId.Value);
            
            if (!string.IsNullOrEmpty(filter.Type))
                query = query.Where(a => a.Type.ToString() == filter.Type);
            
            if (filter.FromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);
            
            if (filter.ToDate.HasValue)
                query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);

            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(50) // Limit to recent 50 activities
                .Select(a => new ActivityReadDto
                {
                    Id = a.Id,
                    Type = a.Type.ToString(),
                    Description = a.Description,
                    Username = a.User.Username,
                    TeamId = a.TeamId,
                    TeamName = a.Team != null ? a.Team.Name : string.Empty,
                    TodoId = a.TodoId,
                    TodoName = a.Todo != null ? a.Todo.Name : string.Empty,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(activities);
        }

        [HttpPost]
        public async Task<IActionResult> CreateActivity(Activity activity)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            activity.UserId = userId;
            activity.CreatedAt = DateTime.UtcNow;

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
} 