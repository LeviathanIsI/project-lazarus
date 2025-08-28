# Simple LoRA Integration Test
Write-Host "=== LoRA Integration Test ===" -ForegroundColor Green

$lorasPath = "$env:LOCALAPPDATA\Lazarus\models\loras"
Write-Host "Testing LoRA directory: $lorasPath" -ForegroundColor Yellow

# Ensure directory exists
if (-not (Test-Path $lorasPath)) {
    New-Item -Path $lorasPath -ItemType Directory -Force
    Write-Host "Created LoRA directory" -ForegroundColor Green
}

# Create test files
$testFile = Join-Path $lorasPath "test_style_lora.safetensors"
Set-Content -Path $testFile -Value "TEST_LORA_CONTENT" -Encoding UTF8

$configContent = @"
{
    "peft_type": "LORA",
    "r": 16,
    "lora_alpha": 16,
    "target_modules": ["q_proj", "v_proj"],
    "task_type": "CAUSAL_LM",
    "description": "Test LoRA for integration testing"
}
"@

$configFile = Join-Path $lorasPath "adapter_config.json"
Set-Content -Path $configFile -Value $configContent -Encoding UTF8

Write-Host "✓ Test files created" -ForegroundColor Green

# Test build
Write-Host "Testing application build..." -ForegroundColor Yellow
dotnet build "App.Desktop\App.Desktop.csproj" --verbosity quiet
Write-Host "Build result: Exit code $LASTEXITCODE" -ForegroundColor Yellow

Write-Host "`n✅ Test complete! Manual testing steps:" -ForegroundColor Cyan
Write-Host "1. Run: dotnet run --project App.Desktop" -ForegroundColor Gray
Write-Host "2. Go to Model Configuration > LoRAs" -ForegroundColor Gray
Write-Host "3. Verify test LoRA is detected" -ForegroundColor Gray
Write-Host "4. Apply the LoRA and check conversations tab parameter panel" -ForegroundColor Gray