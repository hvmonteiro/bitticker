@echo off

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
dotnet publish -c %VERSION% --runtime win-x64 --self-contained
REM echo Build available in bin\Debug\net6.0-windows

exit /b 0
