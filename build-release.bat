@echo off

set DOTNET_CLI_TELEMETRY_OPTOUT=1

REM Check if a parameter was provided
IF "%~1"=="" (
    echo.
    echo Usage: %~nx0 ^<version^>
    echo.
    echo.  version     Expected format: X.Y.Z (e.g., 1.0.0^)
    echo.
    exit /b 1
)


set VERSION=%~1

REM Replace dots with spaces
set TMP=%VERSION:.= %

REM Count number of parts
set COUNT=0
for %%A in (%TMP%) do (
    set /a COUNT+=1
)

REM Check if there are exactly 3 parts
IF %COUNT% NEQ 3 (
    echo Error: Invalid version format. Expected format: X.Y.Z (e.g., 2.0.0^)
    exit /b 1
)

REM Validate each part is a number
for %%A in (%TMP%) do (
    for /f "delims=0123456789" %%B in ("%%A") do (
        echo Error: Invalid version format. Expected format: X.Y.Z (e.g., 2.0.0^)
        exit /b 1
    )
)

REM If valid, echo the version
echo Version provided: %VERSION%

set GITHUB_REF=%VERSION%
set GITHUB_REF_NAME=%VERSION%

REM dotnet dev-certs https --trust
echo Cleaning build environment...
echo.
dotnet clean
echo.
echo Creating new build version...
echo.
dotnet publish --configuration Release -r win-x64 --self-contained true /p:Version=%VERSION% /p:AssemblyVersion=%VERSION%.0 /p:FileVersion=%VERSION%.0 /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true

echo.
echo Creating ZIP package...
del /F BitTicker-v%VERSION%-win-x64.zip >nul 2>nul
tar.exe acf BitTicker-v%VERSION%-win-x64.zip -C bin/Release/net6.0-windows/win-x64/publish *
echo.
echo Build available as a ZIP Package: BitTicker-v%VERSION%-win-x64.zip


exit /b 0
