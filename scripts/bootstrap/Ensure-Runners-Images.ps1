param()

# Idempotently ensure %LOCALAPPDATA%\Lazarus\Runners\Images\stable-diffusion exists and report status
$imagesRoot = Join-Path $env:LOCALAPPDATA 'Lazarus\Runners\Images'
if (-not (Test-Path -LiteralPath $imagesRoot)) {
  New-Item -ItemType Directory -Force -Path $imagesRoot | Out-Null
}

$path = Join-Path $imagesRoot 'stable-diffusion'
if (-not (Test-Path -LiteralPath $path)) {
  New-Item -ItemType Directory -Force -Path $path | Out-Null
  Write-Output "$path  Created"
} else {
  Write-Output "$path  Exists"
}
