using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using TodoApi.DTOs;
using TodoApi.Models;
using System.Text;
using System.Text.Json;

namespace TodoApi.Tests
{
    public class IntegrationTests : TestBase
    {
        [Fact]
        public async Task CreateTodo_ShouldTriggerSignalRNotification()
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

            // Create todo via API
            var todoCreateDto = new TodoCreateDto
            {
                Name = "Integration Test Todo",
                Description = "Created via API",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "integration,test",
                TeamId = team.Id
            };

            var json = JsonSerializer.Serialize(todoCreateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await Client.PostAsync("/api/todo", content);

            // Assert
            response.Should().BeSuccessful();
            
            // Wait for SignalR notification
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Integration Test Todo");
            receivedTodo.TeamId.Should().Be(team.Id);
        }

        [Fact]
        public async Task UpdateTodo_ShouldTriggerSignalRNotification()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Create a todo first
            var todoCreateDto = new TodoCreateDto
            {
                Name = "Original Todo",
                Description = "Original Description",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "original",
                TeamId = team.Id
            };

            var createJson = JsonSerializer.Serialize(todoCreateDto);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var createResponse = await Client.PostAsync("/api/todo", createContent);
            var createdTodo = JsonSerializer.Deserialize<TodoReadDto>(await createResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Set up message listener for update
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoUpdated");

            // Update todo via API
            var todoUpdateDto = new TodoUpdateDto
            {
                Name = "Updated Todo",
                Description = "Updated Description",
                Status = "InProgress",
                Priority = 2
            };

            var updateJson = JsonSerializer.Serialize(todoUpdateDto);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

            // Act
            var updateResponse = await Client.PutAsync($"/api/todo/{createdTodo.Id}", updateContent);

            // Assert
            updateResponse.Should().BeSuccessful();
            
            // Wait for SignalR notification
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Updated Todo");
            receivedTodo.Status.Should().Be("InProgress");
        }

        [Fact]
        public async Task DeleteTodo_ShouldTriggerSignalRNotification()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Create a todo first
            var todoCreateDto = new TodoCreateDto
            {
                Name = "Todo to Delete",
                Description = "Will be deleted",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "delete",
                TeamId = team.Id
            };

            var createJson = JsonSerializer.Serialize(todoCreateDto);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var createResponse = await Client.PostAsync("/api/todo", createContent);
            var createdTodo = JsonSerializer.Deserialize<TodoReadDto>(await createResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Set up message listener for delete
            var messageTask = signalRClient.WaitForMessageAsync<int>("TodoDeleted");

            // Act
            var deleteResponse = await Client.DeleteAsync($"/api/todo/{createdTodo.Id}");

            // Assert
            deleteResponse.Should().BeSuccessful();
            
            // Wait for SignalR notification
            var receivedTodoId = await messageTask;
            receivedTodoId.Should().Be(createdTodo.Id);
        }

        [Fact]
        public async Task AddTeamMember_ShouldTriggerSignalRNotification()
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

            // Add member via API
            var addMemberDto = new AddMemberDto
            {
                UserId = 999, // New user ID
                Role = "Member"
            };

            var json = JsonSerializer.Serialize(addMemberDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await Client.PostAsync($"/api/team/{team.Id}/members", content);

            // Assert
            response.Should().BeSuccessful();
            
            // Wait for SignalR notification
            var receivedMember = await messageTask;
            receivedMember.Should().NotBeNull();
            receivedMember.UserId.Should().Be(999);
            receivedMember.TeamId.Should().Be(team.Id);
        }

        [Fact]
        public async Task RemoveTeamMember_ShouldTriggerSignalRNotification()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Add a member first
            var addMemberDto = new AddMemberDto
            {
                UserId = 888,
                Role = "Member"
            };

            var addJson = JsonSerializer.Serialize(addMemberDto);
            var addContent = new StringContent(addJson, Encoding.UTF8, "application/json");
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            await Client.PostAsync($"/api/team/{team.Id}/members", addContent);

            // Set up message listener for remove
            var messageTask = signalRClient.WaitForMessageAsync<int>("MemberLeft");

            // Act
            var response = await Client.DeleteAsync($"/api/team/{team.Id}/members/888");

            // Assert
            response.Should().BeSuccessful();
            
            // Wait for SignalR notification
            var receivedUserId = await messageTask;
            receivedUserId.Should().Be(888);
        }

        [Fact]
        public async Task ActivityLogging_ShouldTriggerSignalRNotification()
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

            // Create activity via API
            var activityCreateDto = new ActivityCreateDto
            {
                Type = ActivityType.TodoCreated,
                Description = "Created todo via API",
                TeamId = team.Id,
                TodoId = 1
            };

            var json = JsonSerializer.Serialize(activityCreateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await Client.PostAsync("/api/activity", content);

            // Assert
            response.Should().BeSuccessful();
            
            // Wait for SignalR notification
            var receivedActivity = await messageTask;
            receivedActivity.Should().NotBeNull();
            receivedActivity.Type.Should().Be("TodoCreated");
            receivedActivity.TeamId.Should().Be(team.Id);
        }

        [Fact]
        public async Task MultipleClients_ShouldReceiveNotifications()
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

            // Create todo via API
            var todoCreateDto = new TodoCreateDto
            {
                Name = "Multi-client Todo",
                Description = "Created via API",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "multi-client",
                TeamId = team.Id
            };

            var json = JsonSerializer.Serialize(todoCreateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token1);
            var response = await Client.PostAsync("/api/todo", content);

            // Assert
            response.Should().BeSuccessful();
            
            // Both clients should receive the notification
            var receivedTodo1 = await messageTask1;
            var receivedTodo2 = await messageTask2;

            receivedTodo1.Should().NotBeNull();
            receivedTodo2.Should().NotBeNull();
            receivedTodo1.Name.Should().Be("Multi-client Todo");
            receivedTodo2.Name.Should().Be("Multi-client Todo");
        }

        [Fact]
        public async Task ClientNotInTeam_ShouldNotReceiveNotifications()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            // Don't join any team
            // Set up message listener with a short timeout
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated", TimeSpan.FromSeconds(2));

            // Create todo via API
            var todoCreateDto = new TodoCreateDto
            {
                Name = "Isolated Todo",
                Description = "Created via API",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "isolated",
                TeamId = team.Id
            };

            var json = JsonSerializer.Serialize(todoCreateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await Client.PostAsync("/api/todo", content);

            // Assert
            response.Should().BeSuccessful();
            
            // Should timeout because client is not in the team group
            await Assert.ThrowsAsync<TimeoutException>(async () => await messageTask);
        }

        [Fact]
        public async Task Authentication_ShouldWorkWithSignalR()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);

            // Create authenticated SignalR client
            var signalRClient = await CreateSignalRClientAsync(token);

            // Join the team
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");

            // Create todo via API
            var todoCreateDto = new TodoCreateDto
            {
                Name = "Authenticated Todo",
                Description = "Created with authentication",
                DueDate = DateTime.Now.AddDays(1),
                Priority = 1,
                Tags = "auth",
                TeamId = team.Id
            };

            var json = JsonSerializer.Serialize(todoCreateDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await Client.PostAsync("/api/todo", content);

            // Assert
            response.Should().BeSuccessful();
            
            // Should receive notification
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Authenticated Todo");
        }
    }
} 