### !IMPORTANT! Use PowerShell 7 to run the script.
# Creating zip archives with Windows PowerShell is incompatible with az webapp deploy.

param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ProjectBasePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishBasePath,

    [Parameter()]
    [switch]$Api = $false,

    [Parameter()]
    [switch]$Executor = $false,

    [Parameter()]
    [switch]$Proxy = $false,

    [Parameter()]
    [switch]$Scheduler = $false,

    [Parameter()]
    [switch]$Ui = $false,

    [Parameter()]
    [switch]$Linux = $false,

    [Parameter()]
    [switch]$Win = $false,

    [Parameter()]
    [switch]$FrameworkDependent = $false,

    [Parameter()]
    [switch]$SelfContained = $false,

    [Parameter()]
    [switch]$Zip = $false,

    [Parameter()]
    [switch]$KeepAppsettings = $false
)

function Get-OsShortName()
{
    $osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    $osArchDesc = switch ($osArch)
    {
        arm64 { "arm64" }
        x86 { "x86" }
        x64 { "x64" }
        default { throw [System.NotSupportedException] "Unsupported OS architecture" }
    }
    $osDesc = if ($IsWindows) { "win" }
    elseif ($IsLinux) { "linux" }
    elseif ($IsMacOS) { "osx" }
    else { throw [System.NotSupportedException] "Unsupported OS platform" }
    return "$($osDesc)-$($osArchDesc)"
}

function Publish-WebApp([String]$ProjectPath, [String]$AppName, [String]$Runtime, [switch]$PublishSelfContained, [String[]]$RemoveItems)
{
    $appId = "$($AppName)-$($Runtime)$(If ($SelfContained) { '-self-contained' } Else { '' })"
    Write-Host "Publishing $($appId)"
    $publishPath = Join-Path $PublishBasePath $appid
    Write-Host "Cleaning publish folder"
    Remove-Item $publishPath -Recurse -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue
    Write-Host "Starting build process"
    if ($PublishSelfContained)
    {
        dotnet publish $ProjectPath `
            --configuration Release `
            --runtime $Runtime `
            --self-contained true `
            --output $publishPath `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -maxcpucount:1 `
            --tl:off
    }
    else
    {
        dotnet publish $ProjectPath `
            --configuration Release `
            --runtime $Runtime `
            --output $publishPath `
            --tl:off
    }
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Error publishing $($appId)"
        throw [System.ApplicationException] "Build failed for $($appId)"
    }
    foreach ($item in $RemoveItems)
    {
        Join-Path $publishPath $item | ForEach-Object { Remove-Item $_ -ProgressAction SilentlyContinue }
    }
    if ($Zip)
    {
        $path = Join-Path $publishPath "*"
        $destinationPath = "$($publishPath).zip"
        Write-Host "Compressing zip file"
        Remove-Item $destinationPath -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue
        $ProgressPreference = 'SilentlyContinue'
        Compress-Archive -Path $path -DestinationPath $destinationPath -Force
    }
}

function Publish-WebAppAot([String]$ProjectPath, [String]$AppName, [String[]]$RemoveItems)
{
    $OsName = Get-OsShortName
    $appId = "$($AppName)-$($OsName)-aot"
    Write-Host "Publishing $($appId)"
    $publishPath = Join-Path $PublishBasePath $appId
    Write-Host "Cleaning publish folder"
    Remove-Item $publishPath -Recurse -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue
    Write-Host "Starting build process"
    dotnet publish $ProjectPath `
        --configuration Release `
        --output $publishPath `
        -p:PublishAot=true `
        --tl:off
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Error publishing $($appId)"
        throw [System.ApplicationException] "Build failed for $($appId)"
    }
    foreach ($item in $RemoveItems)
    {
        Join-Path $publishPath $item | ForEach-Object { Remove-Item $_ -ProgressAction SilentlyContinue }
    }
    if ($Zip)
    {
        $path = Join-Path $publishPath "*"
        $destinationPath = "$($publishPath).zip"
        Write-Host "Compressing zip file"
        Remove-Item $destinationPath -Force -ErrorAction SilentlyContinue -ProgressAction SilentlyContinue
        $ProgressPreference = 'SilentlyContinue'
        Compress-Archive -Path $path -DestinationPath $destinationPath -Force
    }
}

$apiProjectPath = Join-Path $ProjectBasePath "Biflow.Ui.Api" "Biflow.Ui.Api.csproj"
$executorProjectPath = Join-Path $ProjectBasePath "Biflow.Executor.WebApp" "Biflow.Executor.WebApp.csproj"
$proxyProjectPath = Join-Path $ProjectBasePath "Biflow.ExecutorProxy.WebApp" "Biflow.ExecutorProxy.WebApp.csproj"
$schedulerProjectPath = Join-Path $ProjectBasePath "Biflow.Scheduler.WebApp" "Biflow.Scheduler.WebApp.csproj"
$uiProjectPath = Join-Path $ProjectBasePath "Biflow.Ui" "Biflow.Ui.csproj"

try
{
    $ProjectBasePath = $ProjectBasePath.TrimEnd([System.IO.Path]::PathSeparator)
    $PublishBasePath = $PublishBasePath.TrimEnd([System.IO.Path]::PathSeparator)
    
    $timer = [System.Diagnostics.Stopwatch]::new()
    $timer.Start()

    [string[]]$removeItems =
        if ($KeepAppsettings) { "appsettings.*.json" }
        else { "appsettings*.json" }

    [string[]]$removeItemsSelfContainedLinux =
        if ($KeepAppsettings) { "appsettings.*.json", "*.pdb", "*.staticwebassets.endpoints.json" }
        else { "appsettings*.json", "*.pdb", "*.staticwebassets.endpoints.json" }

    [string[]]$removeItemsSelfContainedWin =
        if ($KeepAppsettings) { "appsettings.*.json", "*.pdb", "*.staticwebassets.endpoints.json", "web.config" }
        else { "appsettings*.json", "*.pdb", "*.staticwebassets.endpoints.json", "web.config" }

    [string[]]$removeItemsUiSelfContainedLinux =
        if ($KeepAppsettings) { "appsettings.*.json", "*.pdb" }
        else { "appsettings*.json", "*.pdb" }

    [string[]]$removeItemsUiSelfContainedWin =
        if ($KeepAppsettings) { "appsettings.*.json", "*.pdb", "web.config" }
        else { "appsettings*.json", "*.pdb", "web.config" }


    # API
    if ($Api -and $Linux -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $apiProjectPath -AppName "api" -Runtime "linux-x64" -RemoveItems $removeItems
    }
    if ($Api -and $Linux -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $apiProjectPath -AppName "api" -Runtime "linux-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedLinux
    }
    if ($Api -and $Win -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $apiProjectPath -AppName "api" -Runtime "win-x64" -RemoveItems $removeItems
    }
    if ($Api -and $Win -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $apiProjectPath -AppName "api" -Runtime "win-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedWin
    }

    # EXECUTOR
    if ($Executor -and $Linux -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $executorProjectPath -AppName "executor" -Runtime "linux-x64" -RemoveItems $removeItems
    }
    if ($Executor -and $Linux -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $executorProjectPath -AppName "executor" -Runtime "linux-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedLinux
    }
    if ($Executor -and $Win -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $executorProjectPath -AppName "executor" -Runtime "win-x64" -RemoveItems $removeItems
    }
    if ($Executor -and $Win -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $executorProjectPath -AppName "executor" -Runtime "win-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedWin
    }

    # PROXY

    # Always publish AOT proxy if the current and target OS's match.
    if ($Proxy -and (($Win -and $IsWindows) -or ($Linux -and $IsLinux)))
    {
        Publish-WebAppAot -ProjectPath $proxyProjectPath -AppName "proxy" -RemoveItems $removeItemsSelfContainedLinux
    }
    if ($Proxy -and $Linux -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $proxyProjectPath -AppName "proxy" -Runtime "linux-x64" -RemoveItems $removeItems
    }
    if ($Proxy -and $Linux -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $proxyProjectPath -AppName "proxy" -Runtime "linux-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedLinux
    }
    if ($Proxy -and $Win -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $proxyProjectPath -AppName "proxy" -Runtime "win-x64" -RemoveItems $removeItems
    }
    if ($Proxy -and $Win -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $proxyProjectPath -AppName "proxy" -Runtime "win-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedWin
    }

    # SCHEDULER
    if ($Scheduler -and $Linux -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $schedulerProjectPath -AppName "scheduler" -Runtime "linux-x64" -RemoveItems $removeItems
    }
    if ($Scheduler -and $Linux -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $schedulerProjectPath -AppName "scheduler" -Runtime "linux-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedLinux
    }
    if ($Scheduler -and $Win -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $schedulerProjectPath -AppName "scheduler" -Runtime "win-x64" -RemoveItems $removeItems
    }
    if ($Scheduler -and $Win -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $schedulerProjectPath -AppName "scheduler" -Runtime "win-x64" -PublishSelfContained -RemoveItems $removeItemsSelfContainedWin
    }

    # UI
    if ($Ui -and $Linux -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $uiProjectPath -AppName "ui" -Runtime "linux-x64" -RemoveItems $removeItems
    }
    if ($Ui -and $Linux -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $uiProjectPath -AppName "ui" -Runtime "linux-x64" -PublishSelfContained -RemoveItems $removeItemsUiSelfContainedLinux
    }
    if ($Ui -and $Win -and $FrameworkDependent)
    {
        Publish-WebApp -ProjectPath $uiProjectPath -AppName "ui" -Runtime "win-x64" -RemoveItems $removeItems
    }
    if ($Ui -and $Win -and $SelfContained)
    {
        Publish-WebApp -ProjectPath $uiProjectPath -AppName "ui" -Runtime "win-x64" -PublishSelfContained -RemoveItems $removeItemsUiSelfContainedWin
    }

    $timer.Stop()
    Write-Host "Publish finished in $($timer.Elapsed)"
}
catch
{
    Write-Host "Error publishing one or more apps"
    Write-Host $_.Exception.Message
}