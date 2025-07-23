# TODO API Quick Start Script
# This script helps you quickly start and test the TODO API

Write-Host "=== TODO API Quick Start ===" -ForegroundColor Green
Write-Host ""

# Check if API is already running
$apiProcess = Get-Process -Name "TodoApi" -ErrorAction SilentlyContinue
if ($apiProcess) {
    Write-Host "⚠️  API is already running. Stopping existing process..." -ForegroundColor Yellow
    Stop-Process -Name "TodoApi" -Force
    Start-Sleep -Seconds 2
}

Write-Host "1. Starting the API..." -ForegroundColor Cyan
Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "TodoApi" -WindowStyle Minimized

Write-Host "2. Waiting for API to start..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

Write-Host "3. Testing API health..." -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "https://localhost:7001/swagger" -Method Get -SkipCertificateCheck
    Write-Host "✅ API is running successfully!" -ForegroundColor Green
} catch {
    Write-Host "❌ API might not be ready yet. Please wait a moment and try again." -ForegroundColor Red
}

Write-Host ""
Write-Host "4. Opening web applications..." -ForegroundColor Cyan

# Open the main web application
Start-Process "https://localhost:7001/todo-app.html"

# Open the SignalR test page
Start-Process "https://localhost:7001/signalr-test.html"

# Open Swagger documentation
Start-Process "https://localhost:7001/swagger"

Write-Host ""
Write-Host "=== Quick Start Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "🌐 Web Applications Opened:" -ForegroundColor Yellow
Write-Host "   • Main App: https://localhost:7001/todo-app.html" -ForegroundColor White
Write-Host "   • SignalR Test: https://localhost:7001/signalr-test.html" -ForegroundColor White
Write-Host "   • API Docs: https://localhost:7001/swagger" -ForegroundColor White
Write-Host ""
Write-Host "📋 Next Steps:" -ForegroundColor Yellow
Write-Host "   1. Register/Login in the main app" -ForegroundColor White
Write-Host "   2. Create a team" -ForegroundColor White
Write-Host "   3. Create shared todos" -ForegroundColor White
Write-Host "   4. Test real-time collaboration" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Troubleshooting:" -ForegroundColor Yellow
Write-Host "   • If pages don't load, wait a moment and refresh" -ForegroundColor White
Write-Host "   • Check console for any errors" -ForegroundColor White
Write-Host "   • Ensure no other process is using port 7001" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Yellow
Write-Host "   • Web App Guide: WEB_APP_GUIDE.md" -ForegroundColor White
Write-Host "   • SignalR Testing: SIGNALR_TESTING.md" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 