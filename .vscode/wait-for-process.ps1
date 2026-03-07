param(
    [Parameter(Mandatory = $true)]
    [string]$ProcessName,

    [int]$TimeoutSeconds = 600,

    [int]$PollMilliseconds = 500
)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)

while ((Get-Date) -lt $deadline) {
    $processMatches = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue

    if ($processMatches) {
        exit 0
    }

    Start-Sleep -Milliseconds $PollMilliseconds
}

Write-Error "Timed out waiting for process '$ProcessName'."
exit 1
