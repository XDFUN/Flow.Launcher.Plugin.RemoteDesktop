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

Write-Host "Extracting archive..."

if (-not (Test-Path $EXTRACTION_TARGET)) {
    New-Item -ItemType Directory -Path $EXTRACTION_TARGET | Out-Null
}

try {
    Expand-Archive -Path $TEMP_ZIP -DestinationPath $EXTRACTION_TARGET -Force
} catch {
    Write-Error "Extraction failed"
    exit 1
}

Remove-Item $TEMP_ZIP -Force

Write-Host "Done!"
