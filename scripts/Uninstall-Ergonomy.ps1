$ErrorActionPreference = 'Stop'
if (Get-Process Ergonomy -ErrorAction SilentlyContinue) { throw 'Exit Ergonomy first.' }
Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name Ergonomy -ErrorAction SilentlyContinue
Remove-Item (Join-Path ([Environment]::GetFolderPath('Programs')) 'همیار سلامت.lnk') -ErrorAction SilentlyContinue
$target = Join-Path $env:LOCALAPPDATA 'Programs\Ergonomy'
if (Test-Path $target) { Remove-Item -LiteralPath $target -Recurse -Force }
Write-Host 'Removed program and startup registration. Local settings/history retained.'
