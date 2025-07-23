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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly IActivityService _activityService;
    private readonly IHubContext<TodoHub> _hubContext;

    public TodoController(AppDbContext context, IMapper mapper, IActivityService activityService, IHubContext<TodoHub> hubContext)
    {
        _context = context;
        _mapper = mapper;
        _activityService = activityService;
        _hubContext = hubContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoReadDto>>> GetTodos(
        [FromQuery] string? status, 
        [FromQuery] DateTime? dueDate, 
        [FromQuery] string? sortBy, 
        [FromQuery] string? order)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        // Get user's own todos
        var userTodos = await _context.Todos
            .Where(t => t.UserId == userId)
            .ToListAsync();
        
        // Get shared todos from teams the user is a member of
        var teamIds = await _context.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId)
            .ToListAsync();
        
        var sharedTodos = await _context.Todos
            .Where(t => t.TeamId.HasValue && teamIds.Contains(t.TeamId.Value))
            .ToListAsync();
        
        // Combine and filter
        var allTodos = userTodos.Concat(sharedTodos).DistinctBy(t => t.Id).AsQueryable();
        
        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            var statusEnum = Enum.Parse<TodoStatus>(status);
            allTodos = allTodos.Where(t => t.Status == statusEnum);
        }
        
        if (dueDate.HasValue)
        {
            allTodos = allTodos.Where(t => t.DueDate.Date == dueDate.Value.Date);
        }
        
        // Apply sorting
        if (!string.IsNullOrEmpty(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "duedate":
                    allTodos = order?.ToLower() == "desc" ? 
                        allTodos.OrderByDescending(t => t.DueDate) : 
                        allTodos.OrderBy(t => t.DueDate);
                    break;
                case "status":
                    allTodos = order?.ToLower() == "desc" ? 
                        allTodos.OrderByDescending(t => t.Status) : 
                        allTodos.OrderBy(t => t.Status);
                    break;
                case "name":
                    allTodos = order?.ToLower() == "desc" ? 
                        allTodos.OrderByDescending(t => t.Name) : 
                        allTodos.OrderBy(t => t.Name);
                    break;
                default:
                    allTodos = allTodos.OrderByDescending(t => t.CreatedAt);
                    break;
            }
        }
        else
        {
            allTodos = allTodos.OrderByDescending(t => t.CreatedAt);
        }
        
        var todos = allTodos.ToList();
        return Ok(_mapper.Map<IEnumerable<TodoReadDto>>(todos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TodoReadDto>> GetTodo(int id)
    {
        // Get current user ID from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return BadRequest("Invalid user information");
        }
        
        // Get todo - either user's own or shared todo from team they're a member of
        var todo = await _context.Todos
            .Include(t => t.Team)
            .FirstOrDefaultAsync(t => t.Id == id && 
                (t.UserId == userId || 
                 (t.TeamId.HasValue && _context.TeamMembers
                     .Any(tm => tm.UserId == userId && tm.TeamId == t.TeamId))));
        
        if (todo == null) return NotFound();
        return Ok(_mapper.Map<TodoReadDto>(todo));
    }

    [HttpPost]
    public async Task<ActionResult<TodoReadDto>> CreateTodo(TodoCreateDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var todo = _mapper.Map<Todo>(dto);
        todo.Status = TodoStatus.NotStarted;
        todo.UserId = userId;
        todo.CreatedAt = DateTime.UtcNow;
        
        // Handle shared todos
        if (dto.TeamId.HasValue)
        {
            // Verify user is a member of the team and has appropriate role
            var teamMember = await _context.TeamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == dto.TeamId.Value);
            
            if (teamMember == null)
            {
                return BadRequest("You are not a member of this team");
            }
            
            // Only Members, Admins, and Owners can create shared todos
            if (teamMember.Role == TeamRole.Viewer)
            {
                return BadRequest("Viewers cannot create shared todos. Only Members, Admins, and Owners can create shared todos.");
            }
            
            todo.TeamId = dto.TeamId.Value;
            todo.IsShared = true;
        }
        else
        {
            // Personal todo
            todo.IsShared = false;
        }
        
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();
        
        // Log activity
        if (todo.TeamId.HasValue)
        {
            // Team shared todo
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TodoCreated, 
                $"Created shared todo '{todo.Name}' in team", 
                todo.TeamId, 
                todo.Id);
            
            // Send SignalR notification to team
            var todoDto = _mapper.Map<TodoReadDto>(todo);
            await _hubContext.Clients.Group($"team_{todo.TeamId}").SendAsync("TodoCreated", todoDto);
        }
        else
        {
            // Personal todo
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TodoCreated, 
                $"Created personal todo '{todo.Name}'", 
                null, 
                todo.Id);
        }
        
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, _mapper.Map<TodoReadDto>(todo));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, TodoUpdateDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        // Get todo - either user's own or shared todo from team they're a member of
        var todo = await _context.Todos
            .Include(t => t.Team)
            .FirstOrDefaultAsync(t => t.Id == id && 
                (t.UserId == userId || 
                 (t.TeamId.HasValue && _context.TeamMembers
                     .Any(tm => tm.UserId == userId && tm.TeamId == t.TeamId))));
        
        if (todo == null) return NotFound();
        
        // Check authorization for shared todos
        if (todo.TeamId.HasValue && todo.UserId != userId)
        {
            // For shared todos, only team admins/owners can modify
            var userRole = await _context.TeamMembers
                .Where(tm => tm.UserId == userId && tm.TeamId == todo.TeamId)
                .Select(tm => tm.Role)
                .FirstOrDefaultAsync();
            
            if (userRole != TeamRole.Admin && userRole != TeamRole.Owner)
            {
                return BadRequest("Only team admins can modify shared todos");
            }
        }
        
        // Only update fields that are provided
        if (!string.IsNullOrEmpty(dto.Name))
            todo.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Description))
            todo.Description = dto.Description;
        if (dto.DueDate.HasValue)
            todo.DueDate = dto.DueDate.Value;
        if (!string.IsNullOrEmpty(dto.Status))
            todo.Status = Enum.Parse<TodoStatus>(dto.Status);
        if (dto.Priority.HasValue)
            todo.Priority = dto.Priority.Value;
        if (!string.IsNullOrEmpty(dto.Tags))
            todo.Tags = dto.Tags;
        
        todo.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        // Log activity
        if (todo.TeamId.HasValue)
        {
            // Team shared todo
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TodoUpdated, 
                $"Updated shared todo '{todo.Name}' in team", 
                todo.TeamId, 
                todo.Id);
            
            // Send SignalR notification to team
            var todoDto = _mapper.Map<TodoReadDto>(todo);
            await _hubContext.Clients.Group($"team_{todo.TeamId}").SendAsync("TodoUpdated", todoDto);
        }
        else
        {
            // Personal todo
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TodoUpdated, 
                $"Updated personal todo '{todo.Name}'", 
                null, 
                todo.Id);
        }
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        // Get todo - either user's own or shared todo from team they're a member of
        var todo = await _context.Todos
            .Include(t => t.Team)
            .FirstOrDefaultAsync(t => t.Id == id && 
                (t.UserId == userId || 
                 (t.TeamId.HasValue && _context.TeamMembers
                     .Any(tm => tm.UserId == userId && tm.TeamId == t.TeamId))));
        
        if (todo == null) return NotFound();
        
        // Check authorization for shared todos
        if (todo.TeamId.HasValue && todo.UserId != userId)
        {
            // For shared todos, only team admins/owners can delete
            var userRole = await _context.TeamMembers
                .Where(tm => tm.UserId == userId && tm.TeamId == todo.TeamId)
                .Select(tm => tm.Role)
                .FirstOrDefaultAsync();
            
            if (userRole != TeamRole.Admin && userRole != TeamRole.Owner)
            {
                return BadRequest("Only team admins can delete shared todos");
            }
        }
        
        // Log activity before deleting
        if (todo.TeamId.HasValue)
        {
            // Team shared todo
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TodoDeleted, 
                $"Deleted shared todo '{todo.Name}' from team", 
                todo.TeamId, 
                todo.Id);
            
            // Send SignalR notification to team
            await _hubContext.Clients.Group($"team_{todo.TeamId}").SendAsync("TodoDeleted", todo.Id);
        }
        else
        {
            // Personal todo
            await _activityService.LogActivityAsync(
                userId, 
                ActivityType.TodoDeleted, 
                $"Deleted personal todo '{todo.Name}'", 
                null, 
                todo.Id);
        }
        
        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();
        return NoContent();
    }
} 