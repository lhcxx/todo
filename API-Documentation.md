# 📚 TodoApi with SignalR - API Documentation

> **ASP.NET Core 8.0** • **SignalR** • **JWT Authentication** • **Entity Framework Core**

## 📋 Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Base URL](#base-url)
- [Error Responses](#error-responses)
- [Models](#models)
- [Endpoints](#endpoints)
  - [Authentication](#authentication-endpoints)
  - [Todos](#todo-endpoints)
  - [Teams](#team-endpoints)
  - [Activities](#activity-endpoints)
- [SignalR Real-time Features](#signalr-real-time-features)
- [Examples](#examples)
- [Testing](#testing)

---

## 🎯 Overview

TodoApi is a comprehensive REST API for managing todos with real-time collaboration features. It supports:

- **User Authentication** - JWT-based authentication
- **Todo Management** - CRUD operations with filtering and sorting
- **Team Collaboration** - Multi-user teams with role-based access
- **Real-time Updates** - SignalR for live notifications
- **Activity Tracking** - Comprehensive audit trail
- **Role-based Authorization** - Owner, Admin, Member, Viewer roles

---

## 🔐 Authentication

The API uses **JWT (JSON Web Token)** authentication. Most endpoints require authentication via the `Authorization` header:

```
Authorization: Bearer <your-jwt-token>
```

### Getting a JWT Token

1. **Register** a new user: `POST /api/auth/register`
2. **Login** to get a token: `POST /api/auth/login`
3. **Use the token** in subsequent requests

---

## 🌐 Base URL

```
Development: http://localhost:5050
Production: https://your-domain.com
```

---

## ❌ Error Responses

### Standard Error Format

```json
{
  "message": "Error description",
  "statusCode": 400,
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Common HTTP Status Codes

| Code | Description |
|------|-------------|
| `200` | Success |
| `201` | Created |
| `400` | Bad Request |
| `401` | Unauthorized |
| `403` | Forbidden |
| `404` | Not Found |
| `500` | Internal Server Error |

---

## 📊 Models

### User Models

#### UserRegisterDto
```json
{
  "username": "string",
  "password": "string"
}
```

#### UserLoginDto
```json
{
  "username": "string",
  "password": "string"
}
```

#### UserReadDto
```json
{
  "id": "integer",
  "username": "string"
}
```

### Todo Models

#### TodoCreateDto
```json
{
  "name": "string",
  "description": "string",
  "dueDate": "datetime",
  "priority": "integer (1-3)",
  "tags": "string",
  "teamId": "integer (optional)"
}
```

#### TodoUpdateDto
```json
{
  "name": "string (optional)",
  "description": "string (optional)",
  "dueDate": "datetime (optional)",
  "status": "string (NotStarted|InProgress|Completed)",
  "priority": "integer (optional)",
  "tags": "string (optional)"
}
```

#### TodoReadDto
```json
{
  "id": "integer",
  "name": "string",
  "description": "string",
  "dueDate": "datetime",
  "status": "string",
  "priority": "integer",
  "tags": "string",
  "isShared": "boolean",
  "teamId": "integer (optional)",
  "teamName": "string (optional)",
  "createdBy": "string"
}
```

### Team Models

#### TeamCreateDto
```json
{
  "name": "string",
  "description": "string"
}
```

#### TeamReadDto
```json
{
  "id": "integer",
  "name": "string",
  "description": "string",
  "ownerId": "integer",
  "ownerName": "string",
  "memberCount": "integer"
}
```

#### AddMemberDto
```json
{
  "teamId": "integer",
  "userId": "integer",
  "role": "string (Owner|Admin|Member|Viewer)"
}
```

### Activity Models

#### ActivityCreateDto
```json
{
  "type": "string (TodoCreated|TodoUpdated|TodoCompleted|TodoDeleted|MemberJoined|MemberLeft|TeamCreated)",
  "description": "string",
  "teamId": "integer (optional)",
  "todoId": "integer (optional)"
}
```

#### ActivityReadDto
```json
{
  "id": "integer",
  "type": "string",
  "description": "string",
  "username": "string",
  "teamId": "integer (optional)",
  "teamName": "string (optional)",
  "todoId": "integer (optional)",
  "todoName": "string (optional)",
  "createdAt": "datetime"
}
```

#### ActivityFilterDto
```json
{
  "teamId": "integer (optional)",
  "userId": "integer (optional)",
  "type": "string (optional)",
  "fromDate": "datetime (optional)",
  "toDate": "datetime (optional)"
}
```

---

## 🔗 Endpoints

### Authentication Endpoints

#### Register User
```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "john_doe",
  "password": "secure_password123"
}
```

**Response:**
```json
{
  "message": "User registered successfully",
  "userId": 1,
  "username": "john_doe"
}
```

#### Login User
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_doe",
  "password": "secure_password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### Get Users
```http
GET /api/auth/users
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "id": 1,
    "username": "john_doe"
  },
  {
    "id": 2,
    "username": "jane_smith"
  }
]
```

### Todo Endpoints

#### Get All Todos
```http
GET /api/todo?status=InProgress&dueDate=2024-01-20&sortBy=dueDate&order=asc
Authorization: Bearer <token>
```

**Query Parameters:**
- `status` (optional): Filter by status (`NotStarted`, `InProgress`, `Completed`)
- `dueDate` (optional): Filter by due date (YYYY-MM-DD)
- `sortBy` (optional): Sort field (`dueDate`, `status`, `name`)
- `order` (optional): Sort order (`asc`, `desc`)

**Response:**
```json
[
  {
    "id": 1,
    "name": "Complete API documentation",
    "description": "Write comprehensive API docs",
    "dueDate": "2024-01-20T00:00:00Z",
    "status": "InProgress",
    "priority": 2,
    "tags": "documentation,api",
    "isShared": false,
    "teamId": null,
    "teamName": null,
    "createdBy": "john_doe"
  }
]
```

#### Get Todo by ID
```http
GET /api/todo/{id}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "id": 1,
  "name": "Complete API documentation",
  "description": "Write comprehensive API docs",
  "dueDate": "2024-01-20T00:00:00Z",
  "status": "InProgress",
  "priority": 2,
  "tags": "documentation,api",
  "isShared": false,
  "teamId": null,
  "teamName": null,
  "createdBy": "john_doe"
}
```

#### Create Todo
```http
POST /api/todo
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "New Todo Item",
  "description": "This is a new todo",
  "dueDate": "2024-01-25T00:00:00Z",
  "priority": 2,
  "tags": "important,urgent",
  "teamId": 1
}
```

**Response:**
```json
{
  "id": 2,
  "name": "New Todo Item",
  "description": "This is a new todo",
  "dueDate": "2024-01-25T00:00:00Z",
  "status": "NotStarted",
  "priority": 2,
  "tags": "important,urgent",
  "isShared": true,
  "teamId": 1,
  "teamName": "Development Team",
  "createdBy": "john_doe"
}
```

#### Update Todo
```http
PUT /api/todo/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Updated Todo Item",
  "status": "Completed",
  "priority": 1
}
```

**Response:**
```json
{
  "message": "Todo updated successfully"
}
```

#### Delete Todo
```http
DELETE /api/todo/{id}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "message": "Todo deleted successfully"
}
```

### Team Endpoints

#### Get My Teams
```http
GET /api/team
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "id": 1,
    "name": "Development Team",
    "description": "Software development team",
    "ownerId": 1,
    "ownerName": "john_doe",
    "memberCount": 3
  }
]
```

#### Create Team
```http
POST /api/team
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "New Team",
  "description": "A new team for collaboration"
}
```

**Response:**
```json
{
  "id": 2,
  "name": "New Team",
  "description": "A new team for collaboration",
  "ownerId": 1,
  "ownerName": "john_doe",
  "memberCount": 1
}
```

#### Get Team by ID
```http
GET /api/team/{id}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "id": 1,
  "name": "Development Team",
  "description": "Software development team",
  "ownerId": 1,
  "ownerName": "john_doe",
  "memberCount": 3
}
```

#### Add Team Member
```http
POST /api/team/{id}/members
Authorization: Bearer <token>
Content-Type: application/json

{
  "teamId": 1,
  "userId": 2,
  "role": "Member"
}
```

**Response:**
```json
{
  "message": "Member added successfully"
}
```

#### Remove Team Member
```http
DELETE /api/team/{id}/members/{memberId}
Authorization: Bearer <token>
```

**Response:**
```json
{
  "message": "Member removed successfully"
}
```

### Activity Endpoints

#### Get Activities
```http
GET /api/activity?teamId=1&type=TodoCreated&fromDate=2024-01-01
Authorization: Bearer <token>
```

**Query Parameters:**
- `teamId` (optional): Filter by team ID
- `userId` (optional): Filter by user ID
- `type` (optional): Filter by activity type
- `fromDate` (optional): Filter from date
- `toDate` (optional): Filter to date

**Response:**
```json
[
  {
    "id": 1,
    "type": "TodoCreated",
    "description": "Created todo 'Complete API documentation'",
    "username": "john_doe",
    "teamId": 1,
    "teamName": "Development Team",
    "todoId": 1,
    "todoName": "Complete API documentation",
    "createdAt": "2024-01-15T10:30:00Z"
  }
]
```

#### Create Activity
```http
POST /api/activity
Authorization: Bearer <token>
Content-Type: application/json

{
  "type": "TodoCreated",
  "description": "Created a new todo",
  "teamId": 1,
  "todoId": 1
}
```

**Response:**
```json
{
  "message": "Activity created successfully",
  "activityId": 2
}
```

---

## ⚡ SignalR Real-time Features

The API includes real-time communication via SignalR hub at `/todohub`.

### Connection

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/todohub")
    .build();

await connection.start();
```

### Hub Methods

#### Join Team
```javascript
await connection.invoke("JoinTeam", "1");
```

#### Leave Team
```javascript
await connection.invoke("LeaveTeam", "1");
```

### Hub Events

#### Todo Events
```javascript
// Todo Created
connection.on("TodoCreated", (todo) => {
    console.log("New todo created:", todo);
});

// Todo Updated
connection.on("TodoUpdated", (todo) => {
    console.log("Todo updated:", todo);
});

// Todo Deleted
connection.on("TodoDeleted", (todoId) => {
    console.log("Todo deleted:", todoId);
});
```

#### Activity Events
```javascript
// Activity Added
connection.on("ActivityAdded", (activity) => {
    console.log("New activity:", activity);
});
```

#### Team Member Events
```javascript
// Member Joined
connection.on("MemberJoined", (member) => {
    console.log("Member joined:", member);
});

// Member Left
connection.on("MemberLeft", (userId) => {
    console.log("Member left:", userId);
});
```

---

## 🎯 Examples

### Complete Workflow Example

#### 1. Register and Login
```bash
# Register
curl -X POST "http://localhost:5050/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "developer",
    "password": "password123"
  }'

# Login
curl -X POST "http://localhost:5050/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "developer",
    "password": "password123"
  }'
```

#### 2. Create Team
```bash
curl -X POST "http://localhost:5050/api/team" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Development Team",
    "description": "Software development team"
  }'
```

#### 3. Create Shared Todo
```bash
curl -X POST "http://localhost:5050/api/todo" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Implement API",
    "description": "Build REST API endpoints",
    "dueDate": "2024-01-25T00:00:00Z",
    "priority": 2,
    "tags": "api,development",
    "teamId": 1
  }'
```

#### 4. Real-time Updates
```javascript
// Connect to SignalR
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/todohub")
    .build();

await connection.start();

// Join team
await connection.invoke("JoinTeam", "1");

// Listen for updates
connection.on("TodoCreated", (todo) => {
    console.log("New todo:", todo);
    // Update UI
});
```

---

## 🧪 Testing

### Using Swagger UI

1. Start the API: `dotnet run --project TodoApi`
2. Open: `http://localhost:5050/swagger`
3. Use the interactive documentation to test endpoints

### Using curl

```bash
# Test authentication
curl -X POST "http://localhost:5050/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username": "testuser", "password": "password123"}'

# Test protected endpoint
curl -X GET "http://localhost:5050/api/todo" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Using JavaScript

```javascript
// Login
const loginResponse = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
        username: 'testuser',
        password: 'password123'
    })
});

const { token } = await loginResponse.json();

// Use token for authenticated requests
const todosResponse = await fetch('/api/todo', {
    headers: { 'Authorization': `Bearer ${token}` }
});
```

---

## 🔧 Configuration

### Environment Variables

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=todoapi;User=root;Password=password;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyForJWTTokenGeneration"
  }
}
```

### CORS Configuration

The API is configured to allow all origins in development:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

---

## 📝 Notes

- **JWT Token Expiry**: Tokens expire after 7 days
- **Team Roles**: Owner > Admin > Member > Viewer
- **Real-time Updates**: SignalR automatically notifies team members of changes
- **Activity Logging**: All major actions are logged for audit purposes
- **Database**: Uses MySQL with Entity Framework Core
- **Validation**: Uses FluentValidation for input validation

---

*This documentation was generated for TodoApi v1.0* 