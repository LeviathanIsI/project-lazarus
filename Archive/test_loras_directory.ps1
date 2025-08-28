# Test script to validate LoRAs directory structure
$lorasPath = "$env:LOCALAPPDATA\Lazarus\models\loras"

Write-Host "Testing LoRAs directory structure..." -ForegroundColor Green

# Create test directory if it doesn't exist
if (-not (Test-Path $lorasPath)) {
    New-Item -Path $lorasPath -ItemType Directory -Force
    Write-Host "Created LoRAs directory: $lorasPath" -ForegroundColor Yellow
}

# Create test LoRA files to validate scanning
$testLoraContent = @'
{
    "peft_type": "LORA",
    "r": 16,
    "lora_alpha": 16,
    "target_modules": ["q_proj", "v_proj"],
    "task_type": "CAUSAL_LM",
    "description": "Test LoRA for validation"
}
'@

$testFiles = @(
    "test_style_lora.safetensors",
    "character_lora_v2.safetensors"
)

foreach ($file in $testFiles) {
    $filePath = Join-Path $lorasPath $file
    $configPath = Join-Path $lorasPath "adapter_config.json"
    
    if (-not (Test-Path $filePath)) {
        # Create dummy safetensors file
        Set-Content -Path $filePath -Value "DUMMY_SAFETENSORS_CONTENT" -Encoding UTF8
        Write-Host "Created test file: $file" -ForegroundColor Cyan
    }
}

# Create adapter_config.json
$configPath = Join-Path $lorasPath "adapter_config.json"
if (-not (Test-Path $configPath)) {
    Set-Content -Path $configPath -Value $testLoraContent -Encoding UTF8
    Write-Host "Created adapter_config.json" -ForegroundColor Cyan
}

Write-Host "LoRAs directory test setup complete!" -ForegroundColor Green
Write-Host "Directory: $lorasPath" -ForegroundColor White
Get-ChildItem -Path $lorasPath | Format-Table Name, LastWriteTime, Length