param
(
    [Parameter(Position = 0, Mandatory = $true)]
    [string]$Path,

    [Parameter(Position = 1, Mandatory = $true)]
    [string]$StartTime
);
[DateTime]$VideoStart = [DateTime]::Parse($StartTime);

# Constants
[int]$TIME_LENGTH = 19;
[string]$DATE_FORMAT = 'yyyy-MM-dd HH:mm:ss';

$Lines = @(Get-Content $Path);
$OutputLines = [string[]]::new($Lines.Count + 1);
$OutputLines[0] = '00:00:00 Start'; # YouTube requires a chapter starting at 00:00:00
$OutputFile = [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($Path), [System.IO.Path]::GetFileNameWithoutExtension($Path) + '-Shifted.txt');

for([int]$i = 0; $i -LT $Lines.Count; $i++)
{
    [string]$Line = $Lines[$i];
    [string]$TimeRaw = $Line.Substring(0, $TIME_LENGTH);
    [string]$Title = $Line.Substring($TIME_LENGTH + 1);

    [DateTime]$Time = [DateTime]::ParseExact($TimeRaw, $DATE_FORMAT, [CultureInfo]::InvariantCulture);
    [TimeSpan]$VideoTime = $Time - $VideoStart;
    [string]$OutputLine = "$($VideoTime.ToString('hh\:mm\:ss')) $Title";
    $OutputLines[$i + 1] = $OutputLine;
}
Set-Content -Path $OutputFile -Value $OutputLines;

Write-Host "Shifted list saved to '$OutputFile'";