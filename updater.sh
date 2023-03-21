# # Updates winget packages repository

# # Checkout repo https://github.com/microsoft/winget-pkgs/tree/master/manifests
# # git clone https://github.com/microsoft/winget-pkgs.git

#!/bin/bash
cd ./winget-pkgs

# Create pkgs.json file with [ as first character
echo "[" > ./pkgs.json

# For every folder (a-z)
for folder in */; do
    # For every company in the folder 
    for company in $folder*/; do
        # For every package in the company
        for package in $company*/; do
            # For every version in the package
            latestVersion=0
            for version in $package*/; do
                # Check if version is greater than latest version with format
                if [[ $version > $latestVersion ]]; then
                    latestVersion=$version
                fi
            done
            packageName=${package::-1}
            companyName=${company::-1}
            versionName=${latestVersion::-1}

            # Append to pkgs.json without new line
            packageString="{\"name\":\"$packageName\",\"company\":\"$companyName\",\"version\":\"$versionName\"},"

            echo $packageString >> ./pkgs.json
        done
    done
done

# Write packages to file in json format
echo "]" >> ./pkgs.json

cd ../