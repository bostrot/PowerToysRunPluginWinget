# PowerShell script that downloads latest release from GitHub and installs it to PowerToys Run plugin folder
# Usage: .\installer.ps1

$installLocation = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Winget"

# Get latest release from GitHub
$latestRelease = Invoke-RestMethod -Uri "https://api.github.com/repos/bostrot/PowerToysRunPluginWinget/releases/latest"

# Get download URL for latest release
$downloadUrl = $latestRelease.assets.browser_download_url

# Download latest release
Invoke-WebRequest -Uri $downloadUrl -OutFile "Winget.tmp.zip"

# Unzip latest release
Expand-Archive -Path "Winget.tmp.zip" -DestinationPath "$installLocation.tmp"

# Move files to plugin folder
Move-Item "$installLocation.tmp\Winget" $installLocation

# Remove zip file
Remove-Item "Winget.tmp.zip"

# Remove temporary folder
Remove-Item "$installLocation.tmp" -Recurse