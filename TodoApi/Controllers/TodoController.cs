using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using AutoMapper;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public TodoController(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
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
        
        var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
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
        
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, _mapper.Map<TodoReadDto>(todo));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, TodoUpdateDto dto)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null) return NotFound();
        
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
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        // Get current user ID from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return BadRequest("Invalid user information");
        }
        
        var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo == null) return NotFound();
        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();
        return NoContent();
    }
} 