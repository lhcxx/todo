using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;
using TodoApi.Hubs;
using AutoMapper;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamController : ControllerBase
    {
        private readonly AppDbContext _context;
            private readonly IMapper _mapper;
    private readonly TodoApi.Services.IAuthorizationService _authService;
    private readonly IActivityService _activityService;
    private readonly IHubContext<TodoHub> _hubContext;

    public TeamController(AppDbContext context, IMapper mapper, TodoApi.Services.IAuthorizationService authService, IActivityService activityService, IHubContext<TodoHub> hubContext)
    {
        _context = context;
        _mapper = mapper;
        _authService = authService;
        _activityService = activityService;
        _hubContext = hubContext;
    }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeamReadDto>>> GetMyTeams()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var teams = await _context.TeamMembers
                .Include(tm => tm.Team)
                .Include(tm => tm.Team.Owner)
                .Where(tm => tm.UserId == userId)
                .Select(tm => new TeamReadDto
                {
                    Id = tm.Team.Id,
                    Name = tm.Team.Name,
                    Description = tm.Team.Description,
                    OwnerId = tm.Team.OwnerId,
                    OwnerName = tm.Team.Owner.Username,
                    MemberCount = tm.Team.Members.Count
                })
                .ToListAsync();

            return Ok(teams);
        }

        [HttpPost]
        public async Task<ActionResult<TeamReadDto>> CreateTeam(TeamCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var team = new Team
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = userId
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Add owner as team member
            var teamMember = new TeamMember
            {
                TeamId = team.Id,
                UserId = userId,
                Role = TeamRole.Owner
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Log team creation activity
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TeamCreated, 
                $"Created team '{team.Name}'", 
                team.Id);

            var teamDto = _mapper.Map<TeamReadDto>(team);
            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, teamDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TeamReadDto>> GetTeam(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!await _authService.IsTeamMember(userId, id))
                return BadRequest("You are not a member of this team");

            var team = await _context.Teams
                .Include(t => t.Owner)
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (team == null) return NotFound();

            var teamDto = _mapper.Map<TeamReadDto>(team);
            return Ok(teamDto);
        }

        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMember(int id, AddMemberDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!await _authService.IsTeamAdmin(userId, id))
                return BadRequest("Only team admins can perform this action");

            var existingMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == dto.UserId);

            if (existingMember != null)
                return BadRequest("User is already a member of this team");

            var teamMember = new TeamMember
            {
                TeamId = id,
                UserId = dto.UserId,
                Role = Enum.Parse<TeamRole>(dto.Role)
            };

            _context.TeamMembers.Add(teamMember);
            await _context.SaveChangesAsync();

            // Log member joined activity
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.MemberJoined, 
                $"Added member to team", 
                id);

            // Send SignalR notification to team
            var user = await _context.Users.FindAsync(dto.UserId);
            var memberDto = new TeamMemberDto
            {
                UserId = dto.UserId,
                Username = user?.Username ?? "Unknown",
                TeamId = id,
                Role = dto.Role
            };
            await _hubContext.Clients.Group($"team_{id}").SendAsync("MemberJoined", memberDto);

            return Ok();
        }

        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            if (!await _authService.IsTeamAdmin(userId, id))
                return BadRequest("Only team admins can perform this action");

            var member = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == memberId);

            if (member == null) return NotFound();

            // Log member left activity
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.MemberLeft, 
                $"Removed member from team", 
                id);

            // Send SignalR notification to team
            await _hubContext.Clients.Group($"team_{id}").SendAsync("MemberLeft", memberId);

            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
} 