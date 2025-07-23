# SignalR Real-time Features Testing Guide

This guide explains how to test real-time features using SignalR in your TodoApi application.

## Overview

The test suite covers three main areas:

1. **SignalR Tests** - Direct SignalR hub testing
2. **Integration Tests** - End-to-end API to SignalR testing
3. **Performance Tests** - Load testing and performance validation

## Test Structure

### TestBase.cs
Base class providing common setup for all tests:
- In-memory database configuration
- Test data seeding
- JWT token generation
- SignalR client creation helpers

### SignalRTests.cs
Direct SignalR hub testing covering:
- Team join/leave operations
- Todo CRUD operations with real-time notifications
- Activity logging
- Team member management
- Multiple client scenarios
- Error handling

### IntegrationTests.cs
End-to-end testing covering:
- API endpoints triggering SignalR notifications
- Complete workflows from HTTP requests to real-time updates
- Authentication integration
- Multi-client scenarios

### PerformanceTests.cs
Performance and load testing:
- Concurrent connection handling
- Message throughput
- Large payload handling
- Memory usage monitoring
- Connection resilience

## Running the Tests

### Prerequisites
```bash
# Install test dependencies
dotnet add TodoApi.Tests package Microsoft.AspNetCore.SignalR.Client
dotnet add TodoApi.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add TodoApi.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add TodoApi.Tests package FluentAssertions
dotnet add TodoApi.Tests package Moq
```

### Run All Tests
```bash
dotnet test TodoApi.Tests
```

### Run Specific Test Categories
```bash
# Run only SignalR tests
dotnet test TodoApi.Tests --filter "FullyQualifiedName~SignalRTests"

# Run only integration tests
dotnet test TodoApi.Tests --filter "FullyQualifiedName~IntegrationTests"

# Run only performance tests
dotnet test TodoApi.Tests --filter "FullyQualifiedName~PerformanceTests"
```

### Run Individual Tests
```bash
# Run a specific test
dotnet test TodoApi.Tests --filter "FullyQualifiedName~TodoCreated_ShouldSendMessageToTeamGroup"
```

## Test Scenarios Covered

### 1. SignalR Hub Operations
- **JoinTeam**: Adding connections to team groups
- **LeaveTeam**: Removing connections from team groups
- **TodoCreated**: Broadcasting todo creation to team members
- **TodoUpdated**: Broadcasting todo updates to team members
- **TodoDeleted**: Broadcasting todo deletion to team members
- **ActivityAdded**: Broadcasting activity logs to team members
- **MemberJoined**: Broadcasting new team member notifications
- **MemberLeft**: Broadcasting team member departure notifications

### 2. Integration Scenarios
- **API to SignalR Flow**: Complete workflows from HTTP requests to real-time notifications
- **Authentication**: JWT token validation with SignalR connections
- **Multi-client**: Multiple clients receiving the same notifications
- **Isolation**: Clients not in teams don't receive team notifications
- **Error Recovery**: Handling invalid data and connection issues

### 3. Performance Scenarios
- **Concurrent Connections**: Testing with multiple simultaneous connections
- **Message Throughput**: Rapid message sending and receiving
- **Large Payloads**: Handling large data payloads efficiently
- **Memory Usage**: Monitoring memory consumption under load
- **Connection Resilience**: Testing reconnection scenarios
- **Error Handling**: Ensuring errors don't crash connections

## Key Testing Patterns

### 1. SignalR Client Setup
```csharp
// Create authenticated SignalR client
var signalRClient = await CreateSignalRClientAsync(token);

// Join a team
await signalRClient.Connection.InvokeAsync("JoinTeam", team.Id.ToString());

// Set up message listener
var messageTask = signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");
```

### 2. Message Waiting
```csharp
// Wait for a single message
var receivedTodo = await signalRClient.WaitForMessageAsync<TodoReadDto>("TodoCreated");

// Wait for multiple messages
var receivedTodos = await signalRClient.WaitForMultipleMessagesAsync<TodoReadDto>("TodoCreated", 3);
```

### 3. API Integration
```csharp
// Make API request
var response = await Client.PostAsync("/api/todo", content);

// Wait for SignalR notification
var receivedTodo = await messageTask;
```

### 4. Performance Testing
```csharp
var stopwatch = Stopwatch.StartNew();
// Perform operations
stopwatch.Stop();

// Assert performance
stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
```

## Best Practices

### 1. Test Isolation
- Each test uses a unique in-memory database
- Test data is seeded fresh for each test
- Connections are properly disposed

### 2. Timeout Handling
- Use appropriate timeouts for message waiting
- Handle cases where messages are not received
- Test both positive and negative scenarios

### 3. Error Scenarios
- Test with invalid data
- Test connection failures
- Test authentication failures
- Ensure graceful error handling

### 4. Performance Considerations
- Monitor memory usage
- Test with realistic data sizes
- Measure response times
- Test concurrent operations

## Debugging Tests

### 1. Enable Detailed Logging
```csharp
// In TestBase.cs, add logging configuration
builder.ConfigureLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
```

### 2. Inspect SignalR Connection State
```csharp
// Check connection state
signalRClient.Connection.State.Should().Be(HubConnectionState.Connected);
```

### 3. Debug Message Content
```csharp
// Log received messages
signalRClient.Connection.On<TodoReadDto>("TodoCreated", message =>
{
    Console.WriteLine($"Received: {JsonSerializer.Serialize(message)}");
});
```

## Common Issues and Solutions

### 1. Connection Timeouts
- **Issue**: Tests timeout waiting for messages
- **Solution**: Increase timeout values or check if messages are actually being sent

### 2. Authentication Failures
- **Issue**: SignalR connections fail authentication
- **Solution**: Ensure JWT tokens are valid and properly configured

### 3. Memory Leaks
- **Issue**: Tests consume too much memory
- **Solution**: Properly dispose of SignalR connections and force garbage collection

### 4. Race Conditions
- **Issue**: Tests fail intermittently
- **Solution**: Use proper async/await patterns and wait for operations to complete

## Extending the Test Suite

### 1. Adding New SignalR Methods
```csharp
[Fact]
public async Task NewMethod_ShouldWorkCorrectly()
{
    // Arrange
    var signalRClient = await CreateSignalRClientAsync(token);
    
    // Act
    await signalRClient.Connection.InvokeAsync("NewMethod", parameters);
    
    // Assert
    // Verify expected behavior
}
```

### 2. Adding New Integration Tests
```csharp
[Fact]
public async Task NewApiEndpoint_ShouldTriggerSignalR()
{
    // Arrange
    var signalRClient = await CreateSignalRClientAsync(token);
    var messageTask = signalRClient.WaitForMessageAsync<SomeDto>("SomeMethod");
    
    // Act
    var response = await Client.PostAsync("/api/new-endpoint", content);
    
    // Assert
    response.Should().BeSuccessful();
    var receivedMessage = await messageTask;
    receivedMessage.Should().NotBeNull();
}
```

### 3. Adding Performance Tests
```csharp
[Fact]
public async Task NewPerformanceScenario_ShouldMeetRequirements()
{
    // Arrange
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    // Perform operations
    
    // Assert
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(expectedTime);
}
```

## Continuous Integration

### 1. GitHub Actions Example
```yaml
- name: Run SignalR Tests
  run: |
    dotnet test TodoApi.Tests --logger trx --results-directory TestResults
```

### 2. Test Categories
```csharp
[Fact, Category("SignalR")]
public async Task SignalRTest() { }

[Fact, Category("Integration")]
public async Task IntegrationTest() { }

[Fact, Category("Performance")]
public async Task PerformanceTest() { }
```

This comprehensive testing approach ensures your SignalR real-time features are robust, performant, and reliable in production environments. 