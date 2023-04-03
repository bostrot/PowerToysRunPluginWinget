:: Batch file that starts ps1 script
@echo off

:: Check if PowerToys is running
tasklist /fi "imagename eq PowerToys.exe" | find /i /n "PowerToys.exe" > nul
if %errorlevel% equ 0 (
    echo ======================================================================================================
    echo PowerToys is running. Do you want to close it to install the plugin?
    echo ======================================================================================================
    
    set /p close=Do you want to close PowerToys now? (y/n)
    if %close%==y (
        taskkill /f /im PowerToys.exe
    ) else (
        echo ======================================================================================================
        echo Please close PowerToys and run this script again.
        echo ======================================================================================================
        pause
        exit 1
    )
)

:: Copy Winget folder to PowerToys modules folder
xcopy /E /I /Y "Winget" "C:\Program Files\PowerToys\modules\launcher\Plugins\Winget"

:: Print success message
echo ======================================================================================================
echo Winget plugin installed successfully to PowerToys modules folder. Please restart PowerToys to use it.
echo ======================================================================================================

:: Do you want to restart PowerToys?
set /p restart=Do you want to restart PowerToys now? (y/n)
if %restart%==y (
    taskkill /f /im PowerToys.exe
    start "" "C:\Program Files\PowerToys\PowerToys.exe"
    exit 0
)

echo Done.

pause

:: Signal 7zS.sfx success
exit 0