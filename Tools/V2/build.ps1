<#
.SYNOPSIS
  Builds GU World 2 for Windows (Unity in batch mode), or only checks its content.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\V2\build.ps1            # build to Builds\GuWorld2\GuWorld.exe
  powershell -ExecutionPolicy Bypass -File Tools\V2\build.ps1 -Check     # only check that everything the game loads is there
  powershell -ExecutionPolicy Bypass -File Tools\V2\build.ps1 -Development
#>
param(
    [switch]$Check,
    [switch]$Development,
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
)

$project = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$logs = Join-Path $project "Logs"
New-Item -ItemType Directory -Force $logs | Out-Null
$log = Join-Path $logs ("v2-" + $(if ($Check) { "check" } else { "build" }) + "-" + (Get-Date -Format "yyyyMMdd-HHmmss") + ".log")
$method = if ($Check) { "GuWorld.Editor.GuWorldBuild.Check" } else { "GuWorld.Editor.GuWorldBuild.Windows" }
$arguments = @("-batchmode", "-projectPath", "`"$project`"", "-executeMethod", $method, "-logFile", "`"$log`"", "-quit")
if ($Development) { $arguments += "-development" }

if (Get-Process -Name Unity -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*\Editor\Unity.exe" }) {
    Write-Host "Unity is open: close it first (one editor per project)."
    exit 2
}

Write-Host "Running $method (log: $log)"
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -Wait
Write-Host "---- GU World lines of the log ----"
Select-String -Path $log -Pattern "GU World|error CS|Build succeeded|Build failed" | Select-Object -Last 40 | ForEach-Object { $_.Line }
Write-Host "Exit code: $($process.ExitCode)"
exit $process.ExitCode
