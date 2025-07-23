using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using TodoApi.DTOs;
using TodoApi.Models;
using System.Text.Json;

namespace TodoApi.Tests
{
    public class SignalRTests : TestBase
    {
        [Fact]
        public async Task JoinTeam_ShouldAddConnectionToGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Act
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Assert
            // The connection should be added to the group
            // We can verify this by checking if the connection receives messages sent to the group
            signalRClient.Connection.State.Should().Be(HubConnectionState.Connected);
        }

        [Fact]
        public async Task LeaveTeam_ShouldRemoveConnectionFromGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team first
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Act
            await signalRClient.Connection.InvokeAsync("LeaveTeam", team.Id.ToString());

            // Assert
            signalRClient.Connection.State.Should().Be(HubConnectionState.Connected);
        }

        [Fact]
        public async Task TodoCreated_ShouldSendMessageToTeamGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");

            // Create a test todo
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Test Todo",
                Description = "Test Description",
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "test",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            // Act
            await signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo);

            // Assert
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Test Todo");
            receivedTodo.TeamId.Should().Be(team.Id);
        }

        [Fact]
        public async Task TodoUpdated_ShouldSendMessageToTeamGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(token);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoUpdated");

            // Create a test todo
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Updated Todo",
                Description = "Updated Description",
                DueDate = DateTime.Now.AddDays(2),
                Status = "InProgress",
                Priority = 2,
                Tags = "updated",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            // Act
            await signalRClient.Connection.InvokeAsync("TodoUpdated", team.Id, todo);

            // Assert
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Updated Todo");
            receivedTodo.Status.Should().Be("InProgress");
        }

        [Fact]
        public async Task TodoDeleted_ShouldSendMessageToTeamGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<int>("TodoDeleted");

            // Act
            await signalRClient.Connection.InvokeAsync("TodoDeleted", team.Id, 1);

            // Assert
            var receivedTodoId = await messageTask;
            receivedTodoId.Should().Be(1);
        }

        [Fact]
        public async Task ActivityAdded_ShouldSendMessageToTeamGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<ActivityReadDto>("ActivityAdded");

            // Create a test activity
            var activity = new ActivityReadDto
            {
                Id = 1,
                Type = "TodoCreated",
                Description = "Created todo 'Test Todo'",
                Username = user.Username,
                TeamId = team.Id,
                TeamName = team.Name,
                TodoId = 1,
                TodoName = "Test Todo",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            await signalRClient.Connection.InvokeAsync("ActivityAdded", team.Id, activity);

            // Assert
            var receivedActivity = await messageTask;
            receivedActivity.Should().NotBeNull();
            receivedActivity.Type.Should().Be("TodoCreated");
            receivedActivity.TeamId.Should().Be(team.Id);
        }

        [Fact]
        public async Task MemberJoined_ShouldSendMessageToTeamGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TeamMemberDto>("MemberJoined");

            // Create a test team member
            var member = new TeamMemberDto
            {
                Id = 1,
                TeamId = team.Id,
                UserId = user.Id,
                Role = "Member",
                Username = user.Username
            };

            // Act
            await signalRClient.Connection.InvokeAsync("MemberJoined", team.Id, member);

            // Assert
            var receivedMember = await messageTask;
            receivedMember.Should().NotBeNull();
            receivedMember.Username.Should().Be(user.Username);
            receivedMember.TeamId.Should().Be(team.Id);
        }

        [Fact]
        public async Task MemberLeft_ShouldSendMessageToTeamGroup()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<int>("MemberLeft");

            // Act
            await signalRClient.Connection.InvokeAsync("MemberLeft", team.Id, user.Id);

            // Assert
            var receivedUserId = await messageTask;
            receivedUserId.Should().Be(user.Id);
        }

        [Fact]
        public async Task MultipleClients_ShouldReceiveMessages()
        {
            // Arrange
            var user1 = await GetTestUserAsync("testuser1");
            var user2 = await GetTestUserAsync("testuser2");
            var team = await GetTestTeamAsync();
            var token1 = GenerateJwtToken(user1);
            var token2 = GenerateJwtToken(user2);

            var client1 = await CreateSignalRClientAsync(token1);
            var client2 = await CreateSignalRClientAsync(token2);

            // Both clients join the team
            await client1.Connection.InvokeAsync("JoinTeam", team.Id.ToString());
            await client2.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listeners for both clients
            var messageTask1 = client1.WaitForMessageAsync<TodoReadDto>("TodoCreated");
            var messageTask2 = client2.WaitForMessageAsync<TodoReadDto>("TodoCreated");

            // Create a test todo
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Shared Todo",
                Description = "Shared Description",
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "shared",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user1.Username
            };

            // Act
            await client1.Connection.InvokeAsync("TodoCreated", team.Id, todo);

            // Assert
            var receivedTodo1 = await messageTask1;
            var receivedTodo2 = await messageTask2;

            receivedTodo1.Should().NotBeNull();
            receivedTodo2.Should().NotBeNull();
            receivedTodo1.Name.Should().Be("Shared Todo");
            receivedTodo2.Name.Should().Be("Shared Todo");
        }

        [Fact]
        public async Task ClientNotInTeam_ShouldNotReceiveMessages()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Don't join any team
            // Set up message listener with a short timeout
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated", TimeSpan.FromSeconds(2));

            // Create a test todo
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Test Todo",
                Description = "Test Description",
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "test",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            // Act & Assert
            await signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo);

            // Should timeout because client is not in the team group
            await Assert.ThrowsAsync<TimeoutException>(async () => await messageTask);
        }

        [Fact]
        public async Task ConnectionDisconnection_ShouldHandleGracefully()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Act
            await signalRClient.Connection.StopAsync();

            // Assert
            signalRClient.Connection.State.Should().Be(HubConnectionState.Disconnected);
        }

        [Fact]
        public async Task InvalidTeamId_ShouldNotThrowException()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Act & Assert
            await signalRClient.Connection.InvokeAsync("JoinTeam", "invalid_team_id");
            signalRClient.Connection.State.Should().Be(HubConnectionState.Connected);
        }

        [Fact]
        public async Task ConcurrentOperations_ShouldHandleMultipleMessages()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener for multiple messages
            var messageTask = signalRClient.WaitForMultipleMessagesAsync<TodoReadDto>("TodoCreated", 3);

            // Create multiple todos
            var todos = new[]
            {
                new TodoReadDto { Id = 1, Name = "Todo 1", TeamId = team.Id, CreatedBy = user.Username },
                new TodoReadDto { Id = 2, Name = "Todo 2", TeamId = team.Id, CreatedBy = user.Username },
                new TodoReadDto { Id = 3, Name = "Todo 3", TeamId = team.Id, CreatedBy = user.Username }
            };

            // Act
            var tasks = todos.Select(todo => signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo));
            await Task.WhenAll(tasks);

            // Assert
            var receivedTodos = await messageTask;
            receivedTodos.Should().HaveCount(3);
            receivedTodos.Should().Contain(t => t.Name == "Todo 1");
            receivedTodos.Should().Contain(t => t.Name == "Todo 2");
            receivedTodos.Should().Contain(t => t.Name == "Todo 3");
        }
    }
} 