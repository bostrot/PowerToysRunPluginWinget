# Script for copying the release files and creating the release zip file
# C:\Users\erict\PowerToys-dev\x64\Release\RunPlugins\Winget
$version = "1.2.3"
$release = "C:\Users\erict\Downloads\winget-powertoys-$version.zip"
$zip = "C:\Program Files\7-Zip\7z.exe"
$path = "C:\Users\erict\PowerToys-dev\x64\Release\RunPlugins\Winget"

# pack the files from path and excluding
&$zip a -aoa -bb0 -bso0 -xr!PowerToys* -xr!Backup* -xr!Ijwhost* -tzip $release $path
