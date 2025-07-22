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
    public async Task<ActionResult<IEnumerable<TodoReadDto>>> GetTodos([FromQuery] string status, [FromQuery] DateTime? dueDate, [FromQuery] string sortBy, [FromQuery] string order)
    {
        // Get current user ID from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return BadRequest("Invalid user information");
        }
        
        var query = _context.Todos.Where(t => t.UserId == userId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status.ToString() == status);
        if (dueDate.HasValue)
            query = query.Where(t => t.DueDate.Date == dueDate.Value.Date);
        if (!string.IsNullOrEmpty(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "duedate":
                    query = order == "desc" ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate);
                    break;
                case "status":
                    query = order == "desc" ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status);
                    break;
                case "name":
                    query = order == "desc" ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name);
                    break;
            }
        }
        var todos = await query.ToListAsync();
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
        var todo = _mapper.Map<Todo>(dto);
        todo.Status = TodoStatus.NotStarted;
        
        // Set UserId from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return BadRequest("Invalid user information");
        }
        todo.UserId = userId;
        
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, _mapper.Map<TodoReadDto>(todo));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, TodoUpdateDto dto)
    {
        // Get current user ID from JWT claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return BadRequest("Invalid user information");
        }
        
        var todo = await _context.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        if (todo == null) return NotFound();
        _mapper.Map(dto, todo);
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