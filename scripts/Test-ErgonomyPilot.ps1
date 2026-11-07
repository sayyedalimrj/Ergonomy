param([string]$Exe=(Join-Path $PSScriptRoot 'Ergonomy.exe'), [int]$Samples=30)
$ErrorActionPreference='Stop'
$env:ERGONOMY_UI_OUTPUT=Join-Path $PWD 'Ergonomy-pilot-evidence'
New-Item -ItemType Directory -Force $env:ERGONOMY_UI_OUTPUT | Out-Null
foreach($arg in @('--self-test','--ui-self-test')) {
 $p=Start-Process $Exe -ArgumentList $arg -PassThru
 if(!$p.WaitForExit(90000)){$p.Kill();throw "Timeout: $arg"}
 if($p.ExitCode -ne 0){throw "$arg failed: $($p.ExitCode)"}
}
$p=Start-Process $Exe -PassThru
$rows=@()
for($i=0;$i -lt $Samples;$i++) {
 Start-Sleep -Seconds 2
 $p.Refresh()
 if($p.HasExited){throw "App exited unexpectedly: $($p.ExitCode)"}
 $rows += [pscustomobject]@{Seconds=$i*2;CpuSeconds=$p.TotalProcessorTime.TotalSeconds;WorkingSetMB=[math]::Round($p.WorkingSet64/1MB,2);PrivateMB=[math]::Round($p.PrivateMemorySize64/1MB,2);Handles=$p.HandleCount}
}
$rows|Export-Csv (Join-Path $env:ERGONOMY_UI_OUTPUT 'resources.csv') -NoTypeInformation
Write-Host 'App left running for manual pilot. Complete docs/WINDOWS-PILOT.md. Resource samples are measurements, not leak certification.'
