param()

# Idempotently ensure %LOCALAPPDATA%\Lazarus\Runners\Images exists and report status
$path = Join-Path $env:LOCALAPPDATA 'Lazarus\Runners\Images'
if (-not (Test-Path -LiteralPath $path)) {
  New-Item -ItemType Directory -Force -Path $path | Out-Null
  Write-Output "$path  Created"
} else {
  Write-Output "$path  Exists"
}

