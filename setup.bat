@echo off

SET PROJECT_PATH=%~dp0

REG ADD HKLM /F>nul 2>&1

IF NOT %ERRORLEVEL%==0 goto :no_permissions

:msc_path

set /p GAME_PATH=Paste here path to my summer car without traling slash:

if not exist "%GAME_PATH%/mysummercar.exe" goto invalid_path

setx /m MSCMP_GAME_PATH "%GAME_PATH%"

echo My Summer Car path has been written.

echo Preparing build folder structure.

if not exist %PROJECT_PATH%\bin mkdir %PROJECT_PATH%\bin

if not exist %PROJECT_PATH%\bin\Release mkdir %PROJECT_PATH%\bin\Release
copy %PROJECT_PATH%\3rdparty\steamapi\steam_api64.dll %PROJECT_PATH%\bin\Release
copy %PROJECT_PATH%\data\steam_appid.txt %PROJECT_PATH%\bin\Release
echo Release prepared.

if not exist "%PROJECT_PATH%\bin\Public Release" mkdir "%PROJECT_PATH%\bin\Public Release"
copy %PROJECT_PATH%\3rdparty\steamapi\steam_api64.dll "%PROJECT_PATH%\bin\Public Release"
copy %PROJECT_PATH%\data\steam_appid.txt "%PROJECT_PATH%\bin\Public Release"
echo Public release prepared.

if not exist %PROJECT_PATH%\bin\Debug mkdir %PROJECT_PATH%\bin\Debug
copy %PROJECT_PATH%\3rdparty\steamapi\steam_api64.dll %PROJECT_PATH%\bin\Debug
copy %PROJECT_PATH%\data\steam_appid.txt %PROJECT_PATH%\bin\Debug
echo Debug prepared.

echo Workspace has been setup
pause

goto :eof

:invalid_path

echo Invalid my summer car path. Unable to find %GAME_PATH%/mysummercar.exe.
goto msc_path

:no_permissions
echo Please run setup.bat as administrator.
pause
