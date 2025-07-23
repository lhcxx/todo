# SignalR Real-time Collaboration Testing Guide

This guide will help you test the real-time collaboration features using SignalR in your TODO API.

## Prerequisites

1. Make sure your API is running: `dotnet run --project TodoApi`
2. Ensure you have some test data (users, teams, todos) in your database
3. Have a modern web browser ready

## Testing Methods

### Method 1: Using the Web Test Page (Recommended)

1. **Start your API**:
   ```bash
   dotnet run --project TodoApi
   ```

2. **Open the test page**:
   Navigate to: `https://localhost:7001/signalr-test.html`

3. **Test real-time collaboration**:
   - Open multiple browser tabs/windows with the test page
   - Click "Connect" in each tab
   - Enter the same team ID (e.g., "1") and click "Join Team"
   - Use the buttons to send different types of messages
   - Watch how messages appear in real-time across all tabs

### Method 2: Using PowerShell Script

1. **Run the test script**:
   ```powershell
   .\test-signalr.ps1
   ```

2. **Follow the script's instructions** to test API endpoints that trigger SignalR notifications

### Method 3: Manual API Testing

1. **Get a JWT token**:
   ```bash
   curl -X POST "https://localhost:7001/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{"username": "testuser", "password": "password123"}' \
     -k
   ```

2. **Create a team todo** (triggers SignalR notification):
   ```bash
   curl -X POST "https://localhost:7001/api/todo" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -d '{
       "name": "Test Real-time Todo",
       "description": "This todo will trigger SignalR notifications",
       "priority": "High",
       "dueDate": "2024-12-31T23:59:59Z",
       "teamId": 1
     }' \
     -k
   ```

3. **Update the todo** (triggers SignalR notification):
   ```bash
   curl -X PUT "https://localhost:7001/api/todo/TODO_ID" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -d '{
       "name": "Updated Real-time Todo",
       "description": "This todo was updated",
       "status": "InProgress"
     }' \
     -k
   ```

4. **Delete the todo** (triggers SignalR notification):
   ```bash
   curl -X DELETE "https://localhost:7001/api/todo/TODO_ID" \
     -H "Authorization: Bearer YOUR_TOKEN" \
     -k
   ```

## SignalR Events

The following events are supported:

### Todo Events
- **TodoCreated**: Fired when a new todo is created in a team
- **TodoUpdated**: Fired when a todo is updated in a team
- **TodoDeleted**: Fired when a todo is deleted from a team

### Team Events
- **MemberJoined**: Fired when a new member joins a team
- **MemberLeft**: Fired when a member leaves a team

### Activity Events
- **ActivityAdded**: Fired when a new activity is logged for a team

## Testing Scenarios

### Scenario 1: Multi-user Todo Collaboration
1. Open 3 browser tabs with the test page
2. Connect and join team "1" in all tabs
3. Create a todo via API in one tab
4. Verify all tabs receive the "TodoCreated" event
5. Update the todo via API
6. Verify all tabs receive the "TodoUpdated" event
7. Delete the todo via API
8. Verify all tabs receive the "TodoDeleted" event

### Scenario 2: Team Member Management
1. Open 2 browser tabs with the test page
2. Connect and join team "1" in both tabs
3. Add a member to the team via API
4. Verify both tabs receive the "MemberJoined" event
5. Remove a member from the team via API
6. Verify both tabs receive the "MemberLeft" event

### Scenario 3: Cross-team Isolation
1. Open 2 browser tabs with the test page
2. Connect both tabs
3. Join team "1" in the first tab
4. Join team "2" in the second tab
5. Create a todo for team "1" via API
6. Verify only the first tab receives the "TodoCreated" event
7. Create a todo for team "2" via API
8. Verify only the second tab receives the "TodoCreated" event

## Troubleshooting

### Connection Issues
- Make sure your API is running on the correct port
- Check browser console for connection errors
- Verify CORS is properly configured

### No Events Received
- Ensure you've joined the correct team
- Check that the todo/team operations are successful
- Verify the team ID matches between API calls and SignalR groups

### Browser Compatibility
- Use a modern browser (Chrome, Firefox, Safari, Edge)
- Ensure JavaScript is enabled
- Check for any browser extensions that might block WebSocket connections

## Expected Behavior

When working correctly, you should see:
- Real-time updates across all connected clients in the same team
- No updates for clients in different teams
- Proper event data with relevant information
- Timestamps showing when events occurred
- Connection status indicators

## Debugging

1. **Check browser console** for any JavaScript errors
2. **Monitor API logs** for any server-side errors
3. **Verify database** that todos/teams exist
4. **Test with simple messages** first before complex scenarios 