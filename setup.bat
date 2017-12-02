REM Windows script to setup workspace.

@echo off

if not exist bin mkdir bin

if not exist bin\Release mkdir bin\Release
copy 3rdparty\steamapi\steam_api.dll bin\Release
copy data\steam_appid.txt bin\Release

if not exist bin\Debug mkdir bin\Debug
copy 3rdparty\steamapi\steam_api.dll bin\Debug
copy data\steam_appid.txt bin\Debug

echo Workspace has been setup
