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
        var query = _context.Todos.AsQueryable();
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
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null) return NotFound();
        return Ok(_mapper.Map<TodoReadDto>(todo));
    }

    [HttpPost]
    public async Task<ActionResult<TodoReadDto>> CreateTodo(TodoCreateDto dto)
    {
        var todo = _mapper.Map<Todo>(dto);
        todo.Status = TodoStatus.NotStarted;
        // TODO: Set UserId from JWT claims
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, _mapper.Map<TodoReadDto>(todo));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTodo(int id, TodoUpdateDto dto)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null) return NotFound();
        _mapper.Map(dto, todo);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null) return NotFound();
        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();
        return NoContent();
    }
} 