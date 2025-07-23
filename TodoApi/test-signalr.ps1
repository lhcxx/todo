# SignalR Real-time Collaboration Test Script
# This script helps you test the real-time collaboration features

Write-Host "=== SignalR Real-time Collaboration Test ===" -ForegroundColor Green
Write-Host ""

# Base URL for the API
$baseUrl = "https://localhost:7001"

Write-Host "1. First, make sure your API is running:" -ForegroundColor Yellow
Write-Host "   dotnet run --project TodoApi" -ForegroundColor Cyan
Write-Host ""

Write-Host "2. Open the SignalR test page in your browser:" -ForegroundColor Yellow
Write-Host "   $baseUrl/signalr-test.html" -ForegroundColor Cyan
Write-Host ""

Write-Host "3. Test Steps:" -ForegroundColor Yellow
Write-Host "   a) Open multiple browser tabs/windows with the test page" -ForegroundColor White
Write-Host "   b) Connect to SignalR in each tab" -ForegroundColor White
Write-Host "   c) Join the same team (e.g., team ID: 1) in each tab" -ForegroundColor White
Write-Host "   d) Send messages from one tab and watch them appear in other tabs" -ForegroundColor White
Write-Host ""

Write-Host "4. Manual API Testing with curl:" -ForegroundColor Yellow
Write-Host ""

# Get JWT token first
Write-Host "Getting JWT token..." -ForegroundColor Cyan
$loginResponse = curl -X POST "$baseUrl/api/auth/login" `
  -H "Content-Type: application/json" `
  -d '{"username": "testuser", "password": "password123"}' `
  -k

$token = ($loginResponse | ConvertFrom-Json).token
Write-Host "Token received: $($token.Substring(0, 20))..." -ForegroundColor Green
Write-Host ""

Write-Host "5. Test creating a team todo (this will trigger SignalR notifications):" -ForegroundColor Yellow
$createTodoResponse = curl -X POST "$baseUrl/api/todo" `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $token" `
  -d '{
    "name": "Test Real-time Todo",
    "description": "This todo will trigger SignalR notifications",
    "priority": "High",
    "dueDate": "2024-12-31T23:59:59Z",
    "teamId": 1
  }' `
  -k

Write-Host "Todo created: $createTodoResponse" -ForegroundColor Green
Write-Host ""

Write-Host "6. Test updating the todo:" -ForegroundColor Yellow
$todoId = ($createTodoResponse | ConvertFrom-Json).id
$updateTodoResponse = curl -X PUT "$baseUrl/api/todo/$todoId" `
  -H "Content-Type: application/json" `
  -H "Authorization: Bearer $token" `
  -d '{
    "name": "Updated Real-time Todo",
    "description": "This todo was updated and should trigger notifications",
    "status": "InProgress"
  }' `
  -k

Write-Host "Todo updated successfully" -ForegroundColor Green
Write-Host ""

Write-Host "7. Test deleting the todo:" -ForegroundColor Yellow
$deleteTodoResponse = curl -X DELETE "$baseUrl/api/todo/$todoId" `
  -H "Authorization: Bearer $token" `
  -k

Write-Host "Todo deleted successfully" -ForegroundColor Green
Write-Host ""

Write-Host "=== Testing Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Check your SignalR test page to see the real-time notifications!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Expected events you should see:" -ForegroundColor Cyan
Write-Host "  - TodoCreated: When the todo was created" -ForegroundColor White
Write-Host "  - TodoUpdated: When the todo was updated" -ForegroundColor White
Write-Host "  - TodoDeleted: When the todo was deleted" -ForegroundColor White
Write-Host ""
Write-Host "If you have multiple browser tabs open and joined the same team," -ForegroundColor Yellow
Write-Host "you should see these events appear in real-time across all tabs!" -ForegroundColor Yellow 