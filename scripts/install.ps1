$REPO = "XDFUN/Flow.Launcher.Plugin.RemoteDesktop"
$VERSION = "" # <-- will be injected by GitHub Actions
$ZIP_NAME = "remote-desktop-$VERSION.zip"

$AppDataFolder = [Environment]::GetFolderPath("ApplicationData")
$EXTRACTION_TARGET = "$AppDataFolder\FlowLauncher\Plugins\RemoteDesktop"

$DOWNLOAD_URL = "https://github.com/$REPO/releases/download/$VERSION/$ZIP_NAME"
$TEMP_ZIP = Join-Path [Environment]::GetFolderPath("TEMP") $ZIP_NAME

Write-Host "Downloading $ZIP_NAME"

try {
    Invoke-WebRequest -Uri $DOWNLOAD_URL -OutFile $TEMP_ZIP
} catch {
    Write-Error "Download failed"
    exit 1
}

if (-not (Test-Path $TEMP_ZIP)) {
    Write-Error "Downloaded file not found"
    exit 1
}

$PROCESS = Get-Process -Name "Flow.Launcher" -ErrorAction SilentlyContinue
$FLOW_LAUNCHER_PATH = $null
$FLOW_LAUNCHER_RUNNING = $false
$RESTART_FLOW_LAUNCHER = $false

if ($PROCESS) {
    try {
        $FLOW_LAUNCHER_PATH = $PROCESS.Path
    } catch {
        $FLOW_LAUNCHER_PATH = $null
    }
    
    $confirm = Read-Host "Flow.Launcher is running. Stop it? (Y/N)"

    if ($confirm -match '^(Y|y)$') {
        Stop-Process -Name "Flow.Launcher" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "Flow.Launcher stopped."
        $RESTART_FLOW_LAUNCHER = $true
    } else {
        $FLOW_LAUNCHER_RUNNING = $true
        Write-Host "Cancelled."
    }
} else {
    Write-Host "Flow.Launcher is not running."
}

if ( -not $FLOW_LAUNCHER_RUNNING ) {
    Write-Host "Extracting archive..."

    if (Test-Path $EXTRACTION_TARGET) {
        Remove-Item (Join-Path $EXTRACTION_TARGET '*') -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (-not (Test-Path $EXTRACTION_TARGET)) {
        New-Item -ItemType Directory -Path $EXTRACTION_TARGET -Force | Out-Null
    }

    try {
        Expand-Archive -Path $TEMP_ZIP -DestinationPath $EXTRACTION_TARGET -Force
    } catch {
        Write-Error "Extraction failed"
        exit 1
    }
}

if ( $RESTART_FLOW_LAUNCHER ) {
    if ($FLOW_LAUNCHER_PATH) {
        Write-Host "Restarting Flow.Launcher"
        Start-Sleep -Seconds 2
        Start-Process $FLOW_LAUNCHER_PATH
    } else {
        Write-Host "Cannot restart Flow.Launcher: path not found."
    }
}

Remove-Item $TEMP_ZIP -Force -ErrorAction SilentlyContinue

Write-Host "Done!"
