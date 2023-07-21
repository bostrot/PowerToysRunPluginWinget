# Script for copying the release files and creating the release zip file
# C:\Users\erict\Sync\Projects\PowerToys\x64\Release\modules\launcher\Plugins\Winget
$version = "1.2.2"
$release = "C:\Users\erict\Downloads\winget-powertoys-$version.zip"
$zip = "C:\Program Files\7-Zip\7z.exe"
$path = "C:\Users\erict\Sync\Projects\PowerToys\x64\Release\modules\launcher\Plugins\Winget"

# pack the files from path and excluding
&$zip a -aoa -bb0 -bso0 -xr!PowerToys* -xr!Backup* -xr!Ijwhost* -tzip $release $path
