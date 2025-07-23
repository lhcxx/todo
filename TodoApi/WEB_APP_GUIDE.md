# TODO API Web Application Guide

This guide will help you use the comprehensive web application for testing all TODO API features.

## Getting Started

### 1. Start the API
```bash
dotnet run --project TodoApi
```

### 2. Open the Web Application
Navigate to: `https://localhost:7001/todo-app.html`

## Features Overview

The web application provides a comprehensive interface for testing all API features:

### 🔐 Authentication
- **Login/Register**: Test user authentication
- **JWT Token Management**: Automatic token handling
- **User Session**: Persistent login state

### 📋 Todo Management
- **Create Todos**: Personal and team-shared todos
- **View Todos**: List all your todos with filtering
- **Edit Todos**: Update todo properties
- **Delete Todos**: Remove todos with confirmation
- **Priority Levels**: Low, Medium, High
- **Due Dates**: Set and track deadlines
- **Tags**: Organize todos with tags

### 👥 Team Management
- **Create Teams**: Set up new teams
- **View Teams**: List all your teams
- **Add Members**: Invite users to teams
- **Role Management**: Member, Admin, Viewer roles
- **Team Isolation**: Separate team workspaces

### 📊 Activity Tracking
- **Activity Feed**: View recent activities
- **Real-time Updates**: Live activity notifications
- **User Actions**: Track who did what
- **Team Activities**: Filter by team

### ⚡ Real-time Collaboration
- **SignalR Connection**: Real-time communication
- **Live Updates**: Instant todo changes
- **Team Notifications**: Member join/leave events
- **Cross-client Sync**: Multiple browser tabs

## Step-by-Step Testing Guide

### Step 1: Authentication
1. **Register a new user** (if needed):
   - Enter username and password
   - Click "Register"
   - You'll see a success message

2. **Login**:
   - Enter your credentials
   - Click "Login"
   - You'll see "Logged in as: [username]"

### Step 2: Create a Team
1. **Navigate to Teams tab**
2. **Fill in team details**:
   - Team Name: "Development Team"
   - Description: "Our development team"
3. **Click "Create Team"**
4. **Click "Load My Teams"** to see your team

### Step 3: Create Shared Todos
1. **Navigate to Todos tab**
2. **Fill in todo details**:
   - Name: "Implement API"
   - Description: "Create REST API endpoints"
   - Priority: High
   - Due Date: Tomorrow
   - Team: Select your team (for shared todo)
3. **Click "Create Todo"**
4. **Click "Load Todos"** to see your todos

### Step 4: Test Real-time Collaboration
1. **Open multiple browser tabs** with the web app
2. **Login in each tab**
3. **Connect to SignalR** in each tab
4. **Join the same team** in each tab
5. **Create/update/delete todos** in one tab
6. **Watch real-time updates** in other tabs

### Step 5: Add Team Members
1. **Navigate to Teams tab**
2. **Select a team** from dropdown
3. **Enter user ID** (create another user first)
4. **Select role** (Member, Admin, Viewer)
5. **Click "Add Member"**

### Step 6: View Activities
1. **Navigate to Activities tab**
2. **Click "Load Activities"**
3. **View recent activities** with timestamps

## Real-time Testing Scenarios

### Scenario 1: Multi-user Todo Collaboration
1. Open 3 browser tabs
2. Login and connect to SignalR in each
3. Join the same team in all tabs
4. Create a todo in tab 1
5. Verify "TodoCreated" appears in tabs 2 & 3
6. Update the todo in tab 2
7. Verify "TodoUpdated" appears in tabs 1 & 3
8. Delete the todo in tab 3
9. Verify "TodoDeleted" appears in tabs 1 & 2

### Scenario 2: Team Member Management
1. Open 2 browser tabs
2. Login and connect to SignalR in each
3. Join the same team in both tabs
4. Add a member via API in tab 1
5. Verify "MemberJoined" appears in tab 2
6. Remove a member via API in tab 1
7. Verify "MemberLeft" appears in tab 2

### Scenario 3: Cross-team Isolation
1. Open 2 browser tabs
2. Login and connect to SignalR in each
3. Join team "1" in tab 1, team "2" in tab 2
4. Create a todo for team "1" via API
5. Verify only tab 1 receives "TodoCreated"
6. Create a todo for team "2" via API
7. Verify only tab 2 receives "TodoCreated"

## API Endpoints Tested

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login

### Todos
- `GET /api/todo` - List todos with filtering
- `POST /api/todo` - Create todo (personal/team)
- `PUT /api/todo/{id}` - Update todo
- `DELETE /api/todo/{id}` - Delete todo

### Teams
- `GET /api/team` - List user's teams
- `POST /api/team` - Create team
- `GET /api/team/{id}` - Get team details
- `POST /api/team/{id}/members` - Add team member
- `DELETE /api/team/{id}/members/{memberId}` - Remove member

### Activities
- `GET /api/activity` - List activities with filtering

### SignalR Hub
- `JoinTeam` - Join team group
- `LeaveTeam` - Leave team group
- `TodoCreated` - Real-time todo creation
- `TodoUpdated` - Real-time todo updates
- `TodoDeleted` - Real-time todo deletion
- `MemberJoined` - Real-time member join
- `MemberLeft` - Real-time member leave

## Troubleshooting

### Connection Issues
- **API not running**: Make sure `dotnet run --project TodoApi` is running
- **CORS errors**: Check browser console for CORS issues
- **Port conflicts**: Verify API is running on port 7001

### Authentication Issues
- **Login fails**: Check username/password
- **Token expired**: Logout and login again
- **Registration fails**: Username might already exist

### Real-time Issues
- **No SignalR events**: Check connection status
- **Events not appearing**: Verify team membership
- **Cross-team events**: Check team isolation

### Data Issues
- **No todos/teams**: Create test data first
- **Missing relationships**: Ensure proper foreign keys
- **Database errors**: Check API logs

## Expected Behavior

### Successful Operations
- ✅ Real-time updates across all connected clients
- ✅ Team-based message isolation
- ✅ Proper error handling and user feedback
- ✅ Persistent login sessions
- ✅ Responsive UI with loading states

### Error Handling
- ❌ Clear error messages for failed operations
- ❌ Graceful handling of network issues
- ❌ Validation feedback for form inputs
- ❌ Confirmation dialogs for destructive actions

## Browser Compatibility

- ✅ Chrome (recommended)
- ✅ Firefox
- ✅ Safari
- ✅ Edge

## Performance Tips

1. **Limit concurrent connections**: Don't open too many tabs
2. **Clear logs periodically**: Use "Clear Log" button
3. **Monitor network**: Check browser dev tools
4. **Test incrementally**: Start with simple operations

## Next Steps

After testing the web application:

1. **Explore advanced features**: Try different team roles
2. **Test edge cases**: Invalid data, network failures
3. **Performance testing**: Multiple concurrent users
4. **Integration testing**: Connect with other applications
5. **Customization**: Modify the UI for your needs

The web application provides a complete testing environment for all TODO API features, making it easy to verify functionality and test real-time collaboration scenarios. 