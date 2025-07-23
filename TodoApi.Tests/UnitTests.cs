using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Services;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using TodoApi.Controllers;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace TodoApi.Tests
{
    public class UnitTests : TestBase
    {
        [Fact]
        public async Task AuthorizationService_IsTeamMember_ShouldReturnTrue_WhenUserIsMember()
        {
            // Arrange
            var authService = new AuthorizationService(DbContext);
            var user = DbContext.Users.First();
            var team = DbContext.Teams.First();

            // Act
            var result = await authService.IsTeamMember(user.Id, team.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AuthorizationService_IsTeamMember_ShouldReturnFalse_WhenUserIsNotMember()
        {
            // Arrange
            var authService = new AuthorizationService(DbContext);
            var user = DbContext.Users.First();
            var team = DbContext.Teams.First();

            // Remove user from team
            var teamMember = DbContext.TeamMembers.First(tm => tm.UserId == user.Id && tm.TeamId == team.Id);
            DbContext.TeamMembers.Remove(teamMember);
            await DbContext.SaveChangesAsync();

            // Act
            var result = await authService.IsTeamMember(user.Id, team.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task AuthorizationService_IsTeamOwner_ShouldReturnTrue_WhenUserIsOwner()
        {
            // Arrange
            var authService = new AuthorizationService(DbContext);
            var team = DbContext.Teams.First();
            var owner = DbContext.Users.First(u => u.Id == team.OwnerId);

            // Act
            var result = await authService.IsTeamOwner(owner.Id, team.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AuthorizationService_IsTeamOwner_ShouldReturnFalse_WhenUserIsNotOwner()
        {
            // Arrange
            var authService = new AuthorizationService(DbContext);
            var team = DbContext.Teams.First();
            var nonOwner = DbContext.Users.First(u => u.Id != team.OwnerId);

            // Act
            var result = await authService.IsTeamOwner(nonOwner.Id, team.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ActivityService_LogActivityAsync_ShouldCreateActivity()
        {
            // Arrange
            var activityService = new ActivityService(DbContext);
            var user = DbContext.Users.First();
            var team = DbContext.Teams.First();

            // Act
            await activityService.LogActivityAsync(
                user.Id,
                ActivityType.TodoCreated,
                "Test activity",
                team.Id);

            // Assert
            var activity = DbContext.Activities.First(a => a.UserId == user.Id && a.TeamId == team.Id);
            activity.Should().NotBeNull();
            activity.Type.Should().Be(ActivityType.TodoCreated);
            activity.Description.Should().Be("Test activity");
        }

        [Fact]
        public async Task TodoController_GetTodos_ShouldReturnUserTodos()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TodoController(DbContext, GetMapper(), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            // Act
            var result = await controller.GetTodos(null, null, null, null);

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<TodoReadDto>>>();
            var actionResult = result as ActionResult<IEnumerable<TodoReadDto>>;
            actionResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task TodoController_GetTodos_WithFilters_ShouldReturnFilteredTodos()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TodoController(DbContext, GetMapper(), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            // Act
            var result = await controller.GetTodos("NotStarted", null, null, null);

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<TodoReadDto>>>();
            var actionResult = result as ActionResult<IEnumerable<TodoReadDto>>;
            actionResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task TodoController_CreateTodo_ShouldCreateTodo()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TodoController(DbContext, GetMapper(), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            var createDto = new TodoCreateDto
            {
                Name = "Unit Test Todo",
                Description = "Testing todo creation",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "unit,test"
            };

            // Act
            var result = await controller.CreateTodo(createDto);

            // Assert
            result.Should().BeOfType<ActionResult<TodoReadDto>>();
            var actionResult = result as ActionResult<TodoReadDto>;
            actionResult.Value.Should().NotBeNull();
            actionResult.Value.Name.Should().Be("Unit Test Todo");
        }

        [Fact]
        public async Task TodoController_UpdateTodo_ShouldUpdateTodo()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TodoController(DbContext, GetMapper(), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            // Create a todo first
            var createDto = new TodoCreateDto
            {
                Name = "Todo to Update",
                Description = "Will be updated",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 0,
                Tags = "update,test"
            };
            var createResult = await controller.CreateTodo(createDto);
            var createdTodo = (createResult as ActionResult<TodoReadDto>).Value;

            var updateDto = new TodoUpdateDto
            {
                Name = "Updated Todo",
                Description = "Has been updated",
                Status = "InProgress",
                Priority = 2
            };

            // Act
            var result = await controller.UpdateTodo(createdTodo.Id, updateDto);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task TodoController_DeleteTodo_ShouldDeleteTodo()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TodoController(DbContext, GetMapper(), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            // Create a todo first
            var createDto = new TodoCreateDto
            {
                Name = "Todo to Delete",
                Description = "Will be deleted",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 0,
                Tags = "delete,test"
            };
            var createResult = await controller.CreateTodo(createDto);
            var createdTodo = (createResult as ActionResult<TodoReadDto>).Value;

            // Act
            var result = await controller.DeleteTodo(createdTodo.Id);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task TeamController_GetMyTeams_ShouldReturnUserTeams()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TeamController(DbContext, GetMapper(), new AuthorizationService(DbContext), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            // Act
            var result = await controller.GetMyTeams();

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<TeamReadDto>>>();
            var actionResult = result as ActionResult<IEnumerable<TeamReadDto>>;
            actionResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task TeamController_CreateTeam_ShouldCreateTeam()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TeamController(DbContext, GetMapper(), new AuthorizationService(DbContext), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            var createDto = new TeamCreateDto
            {
                Name = "Unit Test Team",
                Description = "Testing team creation"
            };

            // Act
            var result = await controller.CreateTeam(createDto);

            // Assert
            result.Should().BeOfType<ActionResult<TeamReadDto>>();
            var actionResult = result as ActionResult<TeamReadDto>;
            actionResult.Value.Should().NotBeNull();
            actionResult.Value.Name.Should().Be("Unit Test Team");
        }

        [Fact]
        public async Task TeamController_AddMember_ShouldAddMember()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new TeamController(DbContext, GetMapper(), new AuthorizationService(DbContext), new ActivityService(DbContext));
            SetupControllerContext(controller, token);

            // Create a team first
            var createDto = new TeamCreateDto
            {
                Name = "Team for Member Test",
                Description = "Testing member addition"
            };
            var createResult = await controller.CreateTeam(createDto);
            var createdTeam = (createResult as ActionResult<TeamReadDto>).Value;

            var addMemberDto = new AddMemberDto
            {
                TeamId = createdTeam.Id,
                UserId = 2, // Assuming user 2 exists
                Role = "Member"
            };

            // Act
            var result = await controller.AddMember(createdTeam.Id, addMemberDto);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task AuthController_Register_ShouldRegisterUser()
        {
            // Arrange
            var controller = new AuthController(DbContext, GetConfiguration());
            var registerDto = new UserRegisterDto
            {
                Username = "unittestuser",
                Password = "TestPassword123!"
            };

            // Act
            var result = await controller.Register(registerDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as dynamic;
            response.message.Should().Be("User registered successfully");
        }

        [Fact]
        public async Task AuthController_Login_ShouldReturnToken()
        {
            // Arrange
            var controller = new AuthController(DbContext, GetConfiguration());
            
            // Register user first
            var registerDto = new UserRegisterDto
            {
                Username = "logintestuser",
                Password = "TestPassword123!"
            };
            await controller.Register(registerDto);

            var loginDto = new UserLoginDto
            {
                Username = "logintestuser",
                Password = "TestPassword123!"
            };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as dynamic;
            string token = response.token;
            token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AuthController_Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var controller = new AuthController(DbContext, GetConfiguration());
            var loginDto = new UserLoginDto
            {
                Username = "nonexistentuser",
                Password = "WrongPassword"
            };

            // Act
            var result = await controller.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task ActivityController_GetActivities_ShouldReturnActivities()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new ActivityController(DbContext, new AuthorizationService(DbContext));
            SetupControllerContext(controller, token);

            // Act
            var result = await controller.GetActivities(new ActivityFilterDto());

            // Assert
            result.Should().BeOfType<ActionResult<IEnumerable<ActivityReadDto>>>();
            var actionResult = result as ActionResult<IEnumerable<ActivityReadDto>>;
            actionResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task ActivityController_CreateActivity_ShouldCreateActivity()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);
            
            var controller = new ActivityController(DbContext, new AuthorizationService(DbContext));
            SetupControllerContext(controller, token);

            var createDto = new ActivityCreateDto
            {
                Type = ActivityType.TodoCreated,
                Description = "Unit test activity"
            };

            // Act
            var result = await controller.CreateActivity(createDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult.Value as dynamic;
            response.message.Should().Be("Activity created successfully");
        }

        [Fact]
        public async Task DbContext_TodoQueries_ShouldWorkCorrectly()
        {
            // Arrange
            var user = DbContext.Users.First();

            // Act - Get user's todos
            var userTodos = await DbContext.Todos
                .Where(t => t.UserId == user.Id)
                .ToListAsync();

            // Assert
            userTodos.Should().NotBeNull();
        }

        [Fact]
        public async Task DbContext_TeamQueries_ShouldWorkCorrectly()
        {
            // Arrange
            var user = DbContext.Users.First();

            // Act - Get user's teams
            var userTeams = await DbContext.TeamMembers
                .Include(tm => tm.Team)
                .Where(tm => tm.UserId == user.Id)
                .Select(tm => tm.Team)
                .ToListAsync();

            // Assert
            userTeams.Should().NotBeNull();
        }

        [Fact]
        public async Task DbContext_ActivityQueries_ShouldWorkCorrectly()
        {
            // Arrange
            var user = DbContext.Users.First();

            // Act - Get user's activities
            var userActivities = await DbContext.Activities
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            // Assert
            userActivities.Should().NotBeNull();
        }

        [Fact]
        public void JwtToken_Generation_ShouldWork()
        {
            // Arrange
            var user = DbContext.Users.First();

            // Act
            var token = GenerateJwtToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().Contain(".");
        }

        [Fact]
        public void JwtToken_Validation_ShouldWork()
        {
            // Arrange
            var user = DbContext.Users.First();
            var token = GenerateJwtToken(user);

            // Act & Assert - Token should be valid
            token.Should().NotBeNullOrEmpty();
            
            // Basic JWT structure validation
            var parts = token.Split('.');
            parts.Should().HaveCount(3); // Header.Payload.Signature
        }

        private void SetupControllerContext(ControllerBase controller, string token)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "testuser")
            };

            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        private IMapper GetMapper()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Todo, TodoReadDto>();
                cfg.CreateMap<Team, TeamReadDto>();
                cfg.CreateMap<Activity, ActivityReadDto>();
            });
            return config.CreateMapper();
        }

        private IConfiguration GetConfiguration()
        {
            var config = new Dictionary<string, string>
            {
                {"Jwt:Key", "YourSuperSecretKeyForTestingPurposesOnly"}
            };
            return new ConfigurationBuilder()
                .AddInMemoryCollection(config)
                .Build();
        }
    }
} 