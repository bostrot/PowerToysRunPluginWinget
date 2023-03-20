# Updates winget packages repository

# Checkout repo https://github.com/microsoft/winget-pkgs/tree/master/manifests
# git clone https://github.com/microsoft/winget-pkgs.git
cd ./winget-pkgs

# Create pkgs.json file with [ as first character
"[" | Out-File -NoNewline -FilePath "./pkgs.json" -Encoding UTF8

# For every folder (a-z)
foreach ($folder in (Get-ChildItem -Path . -Directory)) {
    # For every company in the folder 
    foreach ($company in (Get-ChildItem -Path $folder.FullName -Directory)) {
        # For every package in the company
        foreach ($package in (Get-ChildItem -Path $company.FullName -Directory)) {
            # For every version in the package
            $latestVersion = 0
            foreach ($version in (Get-ChildItem -Path $package.FullName -Directory)) {
                # Check if version is greater than latest version with format 1.0.0.0
                if ($version.Name -gt $latestVersion) {
                    $latestVersion = $version.Name
                }
            }
            $packageName = $package.Name
            $companyName = $company.Name
            $versionName = $latestVersion

            # Append to pkgs.json without new line
            $packageString = "{`"name`":`"$packageName`",`"company`":`"$companyName`",`"version`":`"$versionName`"},"
            
            $packageString | Out-File -NoNewline -FilePath "./pkgs.json" -Encoding UTF8 -Append

            # Write-Host "Added $packageName from $companyName with version $versionName"
        }
    }
}

# Write packages to file in json format
"]" | Out-File -NoNewline -FilePath "./pkgs.json" -Encoding UTF8 -Append

cd ../