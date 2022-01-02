using namespace System.Diagnostics;
$ErrorActionPreference = 'Stop';

# Configure these:
[string]$MainFolder = 'C:\Program Files (x86)\Steam\steamapps\common\Beat Saber';
[string]$GameExeName = 'Beat Saber.exe';
[bool]$WaitForKeyPressToRestore = $true;

# Configure these only if needed:
[string]$GameExe = Join-Path $MainFolder $GameExeName; # The full path to the executable we launch
[string]$GameProcessName = [System.IO.Path]::GetFileNameWithoutExtension($GameExeName); # The process name (without .exe) extension we look for to see if the game is still running
[string]$OldFolder = Join-Path (Split-Path $MainFolder) ((Split-Path $MainFolder -Leaf) + '-old');
[string]$NewFolder = Join-Path (Split-Path $MainFolder) ((Split-Path $MainFolder -Leaf) + '-new');

# Everything below this should not need to be changed.
Write-Host "Main Folder is `"$MainFolder`"";
Write-Host "Old Folder is `"$OldFolder`"";
Write-Host "New Folder is `"$NewFolder`"";

[Process]$GameProcess = [Process]::new();
[ProcessStartInfo]$StartInfo = [ProcessStartInfo]::new();
$StartInfo.FileName = $GameExe;
$StartInfo.WorkingDirectory = $MainFolder;
$StartInfo.UseShellExecute = $false;
$GameProcess.StartInfo = $StartInfo;

function Exit-AfterKey
{
    param
    (
        [Parameter(Position = 0, Mandatory = $true)]
        [string] $ErrorMessage
    );

    Write-Error $ErrorMessage;
    Write-Host 'Press any key to exit...';
    $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') | Out-Null;
    exit;
}

Write-Host 'Preparing folders...'
if (Test-Path $OldFolder)
{
    if ((Test-Path $NewFolder) -AND !(Test-Path $MainFolder))
    {
        Write-Host '-> Old and new found, no current main. Moving old to main.';
        Rename-Item $OldFolder -NewName $MainFolder;
    }
    elseif ((Test-Path $MainFolder) -AND !(Test-Path $NewFolder))
    {
        Write-Host '-> Old and main found, no new. Moving main to new and old to main.';
        Rename-Item $MainFolder -NewName $NewFolder;
        Rename-Item $OldFolder -NewName $MainFolder;
    }
    else { Exit-AfterKey 'Old found, but other 2 folders are in an unrecognized state. Please check and fix manually.'; }
}
elseif (Test-Path $MainFolder)
{
    if (Test-Path $NewFolder) { Write-Host '-> New and main found, no old. No action needed.'; }
    else { Exit-AfterKey 'Main found, but other 2 are missing. Please check and fix manually.'; }
}
Write-Host 'Folder preparation completed.';

Write-Host 'Starting game...';
$GameProcess.Start();
$GameProcess.WaitForExit();
Write-Host 'Main process exited. Waiting for all game processes to exit...';
do { Start-Sleep -Seconds 2; }
while (@(Get-Process $GameProcessName -ErrorAction SilentlyContinue).Count -NE 0);
Write-Host 'All game processes exited.';
Start-Sleep -Seconds 1;

if ($WaitForKeyPressToRestore)
{
    Write-Host 'Press any key to restore folders...';
    $Host.UI.RawUI.ReadKey('NoEcho,IncludeKeyDown') | Out-Null;
}
Write-Host 'Restoring folders...'

if (!(Test-Path $OldFolder))
{
    if ((Test-Path $MainFolder) -AND (Test-Path $NewFolder))
    {
        Write-Host '-> Moving main to old, then new to main.';
        Rename-Item $MainFolder -NewName $OldFolder;
        Rename-Item $NewFolder -NewName $MainFolder;
    }
    elseif (Test-Path $MainFolder)
    {
        Write-Host '-> No new found, only moving main to old.';
        Rename-Item $MainFolder -NewName $OldFolder;
    }
    else { Exit-AfterKey 'The main folder is absent. Not moving anything.'; }
}
else { Exit-AfterKey 'Could not move main back to old, as old already exists. Please check and fix manually.'; }

Write-Host 'Folder restoration completed.';