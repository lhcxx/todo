# SignalR Tests Runner Script
# This script helps you run different types of SignalR tests

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("all", "signalr", "integration", "performance", "e2e", "unit", "validation", "stress")]
    [string]$TestType = "all",
    
    [Parameter(Mandatory=$false)]
    [switch]$Detailed,
    
    [Parameter(Mandatory=$false)]
    [switch]$BuildOnly
)

Write-Host "=== SignalR Real-time Features Test Runner ===" -ForegroundColor Green
Write-Host ""

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build todo.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please fix build errors before running tests." -ForegroundColor Red
    exit 1
}

if ($BuildOnly) {
    Write-Host "Build completed successfully!" -ForegroundColor Green
    exit 0
}

# Run tests based on type
switch ($TestType.ToLower()) {
    "all" {
        Write-Host "Running all SignalR tests..." -ForegroundColor Yellow
        $filter = ""
    }
    "signalr" {
        Write-Host "Running SignalR hub tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~SignalRTests`""
    }
    "integration" {
        Write-Host "Running integration tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~IntegrationTests`""
    }
    "performance" {
        Write-Host "Running performance tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~PerformanceTests`""
    }
    "e2e" {
        Write-Host "Running E2E tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~E2ETests`""
    }
    "unit" {
        Write-Host "Running unit tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~UnitTests`""
    }
    "validation" {
        Write-Host "Running validation tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~ValidationTests`""
    }
    "stress" {
        Write-Host "Running stress tests..." -ForegroundColor Yellow
        $filter = "--filter `"FullyQualifiedName~StressTests`""
    }
    default {
        Write-Host "Invalid test type: $TestType" -ForegroundColor Red
        Write-Host "Valid options: all, signalr, integration, performance" -ForegroundColor Yellow
        exit 1
    }
}

# Build test command
$testCommand = "dotnet test TodoApi.Tests"
if ($filter) {
    $testCommand += " $filter"
}
if ($Detailed) {
    $testCommand += " --verbosity detailed"
}

Write-Host "Executing: $testCommand" -ForegroundColor Cyan
Write-Host ""

# Run the tests
Invoke-Expression $testCommand

$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✅ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Test Summary:" -ForegroundColor Yellow
Write-Host "- SignalR Tests: Direct hub method testing" -ForegroundColor White
Write-Host "- Integration Tests: End-to-end API to SignalR testing" -ForegroundColor White
Write-Host "- Performance Tests: Load and performance validation" -ForegroundColor White
Write-Host ""
Write-Host "For more details, see: TodoApi.Tests/README.md" -ForegroundColor Cyan

exit $exitCode 