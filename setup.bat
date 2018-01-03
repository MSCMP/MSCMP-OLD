@echo off

REG ADD HKLM /F>nul 2>&1

IF NOT %ERRORLEVEL%==0 goto :no_permissions

:msc_path

set /p GAME_PATH=Paste here path to my summer car without traling slash:

if not exist "%GAME_PATH%/mysummercar.exe" goto invalid_path

setx /m MSCMP_GAME_PATH "%GAME_PATH%"

echo My Summer Car path has been written.

echo Preparing build folder structure.

if not exist bin mkdir bin

if not exist bin\Release mkdir bin\Release
copy 3rdparty\steamapi\steam_api.dll bin\Release
copy data\steam_appid.txt bin\Release
echo Release prepared.

if not exist "bin\Public Release" mkdir "bin\Public Release"
copy 3rdparty\steamapi\steam_api.dll "bin\Public Release"
copy data\steam_appid.txt "bin\Public Release"
echo Public release prepared.

if not exist bin\Debug mkdir bin\Debug
copy 3rdparty\steamapi\steam_api.dll bin\Debug
copy data\steam_appid.txt bin\Debug
echo Debug prepared.

echo Workspace has been setup
pause

goto :eof

:invalid_path

echo Invalid my summer car path. Unable to find %GAME_PATH%/mysummercar.exe.
goto msc_path

:no_permissions
echo Please run setup.bat as administrator.
