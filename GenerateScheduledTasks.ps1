$time = Get-Date 0:00am

Do {
    $argument = 'exec-schedules -h ' + $time.Hour + ' -m ' + $time.Minute

    $action = New-ScheduledTaskAction -Execute 'C:\EtlManagerExecutor\EtlManagerExecutor.exe' `
        -Argument $argument `
        -WorkingDirectory 'C:\EtlManagerExecutor\'

    $trigger = New-ScheduledTaskTrigger -Daily -At $time

    $taskName = 'ETLMANAGER_' + $time.ToShortTimeString()

    Register-ScheduledTask -Action $action -Trigger $trigger -TaskName $taskName -TaskPath "\ETL Manager" `        -User 'Domain\User' -Password 'password'

    $time = $time.AddMinutes(15)

} While ($time.Hour -gt 0 -or $time.Minute -gt 0)