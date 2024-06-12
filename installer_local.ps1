# PowerShell script that downloads latest release from GitHub and installs it to PowerToys Run plugin folder
# Usage: .\installer.ps1


$version = "1.3.0"
$release = "$env:USERPROFILE\Downloads\winget-powertoys-$version.zip"
$installLocation = "$env:LOCALAPPDATA\PowerToys\RunPlugins\Winget"

# Unzip latest release
Expand-Archive -Path $release -DestinationPath "$installLocation.tmp"

# Move files to plugin folder
Move-Item "$installLocation.tmp\Winget" $installLocation

# Remove temporary folder
Remove-Item "$installLocation.tmp" -Recurse