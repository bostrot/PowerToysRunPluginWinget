# Script for copying the release files and creating the release zip file
# C:\Users\erict\PowerToys-dev\x64\Release\RunPlugins\Winget
$version = "1.3.0"
$release = "$env:USERPROFILE\Downloads\winget-powertoys-$version.zip"
$zip = "$env:ProgramFiles\7-Zip\7z.exe"
$path = "D:\erict\PowerToys-dev\x64\Release\RunPlugins\Winget"

# pack the files from path and excluding
&$zip a -aoa -bb0 -bso0 -xr!PowerToys* -xr!Backup* -xr!Ijwhost* -tzip $release $path
