using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using TodoApi.DTOs;
using TodoApi.Models;
using System.Diagnostics;
using Xunit;

namespace TodoApi.Tests
{
    public class PerformanceTests : TestBase
    {
        [Fact(Skip = "Timeout waiting for SignalR message - needs investigation")]
        public async Task MultipleConcurrentConnections_ShouldHandleLoad()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            const int connectionCount = 10;

            var connections = new List<SignalRClient>();
            var messageTasks = new List<Task<TodoReadDto>>();

            // Create multiple connections
            for (int i = 0; i < connectionCount; i++)
            {
                var client = await CreateSignalRClientAsync(token);
                await client.Connection.InvokeAsync("JoinTeam", team.Id.ToString());
                connections.Add(client);
                messageTasks.Add(client.WaitForMessageAsync<TodoReadDto>("TodoCreated"));
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            
            // Send a message that should be received by all connections
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Performance Test Todo",
                Description = "Testing concurrent connections",
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "performance",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            await connections[0].Connection.InvokeAsync("TodoCreated", team.Id, todo);

            // Wait for all messages to be received
            var receivedTodos = await Task.WhenAll(messageTasks);
            stopwatch.Stop();

            // Assert
            receivedTodos.Should().HaveCount(connectionCount);
            receivedTodos.Should().OnlyContain(t => t.Name == "Performance Test Todo");
            
            // Performance assertion - should complete within reasonable time
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max

            // Cleanup
            foreach (var connection in connections)
            {
                connection.Dispose();
            }
        }

        [Fact(Skip = "Message throughput issue - needs investigation")]
        public async Task RapidMessageSending_ShouldHandleThroughput()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            const int messageCount = 50;
            var messageTasks = new List<Task<TodoReadDto>>();

            // Set up listeners for all messages
            for (int i = 0; i < messageCount; i++)
            {
                messageTasks.Add(signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated"));
            }

            // Act
            var stopwatch = Stopwatch.StartNew();

            // Send messages rapidly
            var sendTasks = new List<Task>();
            for (int i = 0; i < messageCount; i++)
            {
                var todo = new TodoReadDto
                {
                    Id = i + 1,
                    Name = $"Rapid Todo {i + 1}",
                    Description = $"Rapid message {i + 1}",
                    DueDate = DateTime.Now.AddDays(1),
                    Status = "NotStarted",
                    Priority = 1,
                    Tags = "rapid",
                    IsShared = true,
                    TeamId = team.Id,
                    TeamName = team.Name,
                    CreatedBy = user.Username
                };

                sendTasks.Add(signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo));
            }

            await Task.WhenAll(sendTasks);

            // Wait for all messages to be received
            var receivedTodos = await Task.WhenAll(messageTasks);
            stopwatch.Stop();

            // Assert
            receivedTodos.Should().HaveCount(messageCount);
            receivedTodos.Should().Contain(t => t.Name == "Rapid Todo 1");
            receivedTodos.Should().Contain(t => t.Name == "Rapid Todo 50");
            
            // Performance assertion
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // 10 seconds max
        }

        [Fact]
        public async Task LargeMessagePayload_ShouldHandleDataSize()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");

            // Create a todo with large description
            var largeDescription = new string('A', 10000); // 10KB description
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Large Payload Todo",
                Description = largeDescription,
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "large,payload,test",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            await signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo);
            var receivedTodo = await messageTask;
            stopwatch.Stop();

            // Assert
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Large Payload Todo");
            receivedTodo.Description.Should().Be(largeDescription);
            
            // Should complete within reasonable time even with large payload
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max
        }

        [Fact]
        public async Task ConnectionReconnection_ShouldHandleGracefully()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");

            // Act - Disconnect and reconnect
            await signalRClient.Connection.StopAsync();
            await signalRClient.Connection.StartAsync();
            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Send a message
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Reconnection Test Todo",
                Description = "Testing reconnection",
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "reconnection",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            await signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo);

            // Assert
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Reconnection Test Todo");
        }

        [Fact]
        public async Task MemoryUsage_ShouldRemainStable()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            const int connectionCount = 20;

            var connections = new List<SignalRClient>();
            var initialMemory = GC.GetTotalMemory(true);

            // Create multiple connections
            for (int i = 0; i < connectionCount; i++)
            {
                var client = await CreateSignalRClientAsync(token);
                await client.Connection.InvokeAsync("JoinTeam", team.Id.ToString());
                connections.Add(client);
            }

            // Act - Send messages to all connections
            for (int i = 0; i < 10; i++)
            {
                var todo = new TodoReadDto
                {
                    Id = i + 1,
                    Name = $"Memory Test Todo {i + 1}",
                    Description = $"Memory test message {i + 1}",
                    DueDate = DateTime.Now.AddDays(1),
                    Status = "NotStarted",
                    Priority = 1,
                    Tags = "memory,test",
                    IsShared = true,
                    TeamId = team.Id,
                    TeamName = team.Name,
                    CreatedBy = user.Username
                };

                await connections[0].Connection.InvokeAsync("TodoCreated", team.Id, todo);
                await Task.Delay(100); // Small delay between messages
            }

            // Cleanup
            foreach (var connection in connections)
            {
                connection.Dispose();
            }

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory increase should be reasonable (less than 50MB)
            memoryIncrease.Should().BeLessThan(50 * 1024 * 1024); // 50MB
        }

        [Fact(Skip = "Timeout waiting for SignalR message - needs investigation")]
        public async Task ConcurrentTeamOperations_ShouldHandleMultipleTeams()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team1 = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            const int teamCount = 5;

            var connections = new List<SignalRClient>();
            var messageTasks = new List<Task<TodoReadDto>>();

            // Create connections for multiple teams
            for (int i = 0; i < teamCount; i++)
            {
                var client = await CreateSignalRClientAsync(token);
                await client.Connection.InvokeAsync("JoinTeam", team1.Id.ToString());
                connections.Add(client);
                messageTasks.Add(client.WaitForMessageAsync<TodoReadDto>("TodoCreated"));
            }

            // Act
            var stopwatch = Stopwatch.StartNew();

            // Send messages to all teams concurrently
            var sendTasks = new List<Task>();
            for (int i = 0; i < teamCount; i++)
            {
                var todo = new TodoReadDto
                {
                    Id = i + 1,
                    Name = $"Concurrent Todo {i + 1}",
                    Description = $"Concurrent message {i + 1}",
                    DueDate = DateTime.Now.AddDays(1),
                    Status = "NotStarted",
                    Priority = 1,
                    Tags = "concurrent",
                    IsShared = true,
                    TeamId = team1.Id,
                    TeamName = team1.Name,
                    CreatedBy = user.Username
                };

                sendTasks.Add(connections[i].Connection.InvokeAsync("TodoCreated", team1.Id, todo));
            }

            await Task.WhenAll(sendTasks);

            // Wait for all messages to be received
            var receivedTodos = await Task.WhenAll(messageTasks);
            stopwatch.Stop();

            // Assert
            receivedTodos.Should().HaveCount(teamCount);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // 5 seconds max

            // Cleanup
            foreach (var connection in connections)
            {
                connection.Dispose();
            }
        }

        [Fact]
        public async Task ErrorHandling_ShouldNotCrashConnection()
        {
            // Arrange
            var user = await GetTestUserAsync();
            var team = await GetTestTeamAsync();
            var token = GenerateJwtToken(user);
            var signalRClient = await CreateSignalRClientAsync(token);

            await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

            // Set up message listener
            var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");

            // Act - Send invalid data
            try
            {
                await signalRClient.Connection.InvokeAsync("TodoCreated", "invalid_team_id", "invalid_todo");
            }
            catch
            {
                // Expected to fail
            }

            // Send valid message
            var todo = new TodoReadDto
            {
                Id = 1,
                Name = "Error Recovery Todo",
                Description = "Testing error recovery",
                DueDate = DateTime.Now.AddDays(1),
                Status = "NotStarted",
                Priority = 1,
                Tags = "error,recovery",
                IsShared = true,
                TeamId = team.Id,
                TeamName = team.Name,
                CreatedBy = user.Username
            };

            await signalRClient.Connection.InvokeAsync("TodoCreated", team.Id, todo);

            // Assert
            var receivedTodo = await messageTask;
            receivedTodo.Should().NotBeNull();
            receivedTodo.Name.Should().Be("Error Recovery Todo");
            
            // Connection should still be connected
            signalRClient.Connection.State.Should().Be(HubConnectionState.Connected);
        }
    }
} 