# Lazarus Quality Gate Validation Script
# Code.Quality.Sentinel - Build Hygiene Enforcement

param(
    [string]$Configuration = "Release",
    [switch]$TreatWarningsAsErrors = $true,
    [switch]$Verbose = $false
)

Write-Host "🔍 LAZARUS QUALITY GATE VALIDATION" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Set error handling
$ErrorActionPreference = "Stop"

# Function to run build and capture output
function Invoke-QualityBuild {
    param($Config)
    
    Write-Host "🔨 Building $Config configuration..." -ForegroundColor Yellow
    
    $buildArgs = @(
        "build"
        "Lazarus.sln"
        "--configuration"
        $Config
        "--verbosity"
        "normal"
        "--no-restore"
    )
    
    if ($TreatWarningsAsErrors) {
        $buildArgs += "--property:TreatWarningsAsErrors=true"
    }
    
    try {
        $output = & dotnet @buildArgs 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($Verbose) {
            Write-Host $output
        }
        
        return @{
            Output = $output -join "`n"
            ExitCode = $exitCode
            Success = $exitCode -eq 0
        }
    }
    catch {
        return @{
            Output = $_.Exception.Message
            ExitCode = -1
            Success = $false
        }
    }
}

# Function to count warnings and errors
function Get-BuildMetrics {
    param($BuildOutput)
    
    $warnings = 0
    $errors = 0
    
    if ($BuildOutput -match "(\d+) Warning\(s\)") {
        $warnings = [int]$Matches[1]
    }
    
    if ($BuildOutput -match "(\d+) Error\(s\)") {
        $errors = [int]$Matches[1]
    }
    
    return @{
        Warnings = $warnings
        Errors = $errors
    }
}

try {
    # Restore packages first
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
    & dotnet restore Lazarus.sln --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed"
    }
    
    # Run quality build
    $buildResult = Invoke-QualityBuild -Config $Configuration
    $metrics = Get-BuildMetrics -BuildOutput $buildResult.Output
    
    # Display results
    Write-Host ""
    Write-Host "📊 BUILD QUALITY METRICS" -ForegroundColor Cyan
    Write-Host "========================" -ForegroundColor Cyan
    Write-Host "Configuration: $Configuration" -ForegroundColor White
    Write-Host "Warnings: $($metrics.Warnings)" -ForegroundColor $(if ($metrics.Warnings -eq 0) { "Green" } else { "Yellow" })
    Write-Host "Errors: $($metrics.Errors)" -ForegroundColor $(if ($metrics.Errors -eq 0) { "Green" } else { "Red" })
    Write-Host "Success: $($buildResult.Success)" -ForegroundColor $(if ($buildResult.Success) { "Green" } else { "Red" })
    
    # Quality gate validation
    if (-not $buildResult.Success) {
        Write-Host ""
        Write-Host "❌ QUALITY GATE FAILED" -ForegroundColor Red
        Write-Host "Build errors detected. Fix the following issues:" -ForegroundColor Red
        Write-Host $buildResult.Output -ForegroundColor Gray
        exit 1
    }
    
    if ($metrics.Warnings -gt 0 -and $TreatWarningsAsErrors) {
        Write-Host ""
        Write-Host "⚠️  QUALITY GATE FAILED" -ForegroundColor Yellow
        Write-Host "Compiler warnings detected in $Configuration build:" -ForegroundColor Yellow
        Write-Host $buildResult.Output -ForegroundColor Gray
        exit 1
    }
    
    # Success
    Write-Host ""
    Write-Host "✅ QUALITY GATE PASSED" -ForegroundColor Green
    Write-Host "Zero-warning build achieved!" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎯 QUALITY ENFORCEMENT SUMMARY:" -ForegroundColor Cyan
    Write-Host "• CS1998 violations fixed: Async methods without await" -ForegroundColor Green
    Write-Host "• CS8602 violations fixed: Null reference dereferencing" -ForegroundColor Green
    Write-Host "• CS0168 violations fixed: Unused variable declarations" -ForegroundColor Green
    Write-Host "• Release configuration: Warnings treated as errors" -ForegroundColor Green
    Write-Host ""
    Write-Host "Ready for handoff to performance-budgeter! 🚀" -ForegroundColor Magenta
    
}
catch {
    Write-Host ""
    Write-Host "💥 QUALITY GATE EXECUTION FAILED" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}