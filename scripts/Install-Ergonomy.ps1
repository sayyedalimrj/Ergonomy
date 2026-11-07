$ErrorActionPreference = 'Stop'
$target = Join-Path $env:LOCALAPPDATA 'Programs\Ergonomy'
if (Get-Process Ergonomy -ErrorAction SilentlyContinue) { throw 'Exit Ergonomy from its tray menu before installing.' }
New-Item -ItemType Directory -Force $target | Out-Null
Get-ChildItem $PSScriptRoot | Where-Object Name -ne 'Install-Ergonomy.ps1' | Copy-Item -Destination $target -Recurse -Force
$ws = New-Object -ComObject WScript.Shell
$link = $ws.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Programs')) 'Ergonomy.lnk'))
$link.TargetPath = Join-Path $target 'Ergonomy.exe'
$link.WorkingDirectory = $target
$link.Save()
Write-Host "Installed for this user: $target. Startup remains optional in settings."
