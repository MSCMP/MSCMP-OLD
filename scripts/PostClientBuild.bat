@echo off
rem Script ran after MSCMPClient.dll gets build.
rem The full path to the .dll is passed here as first parameter.

%~dp0\pdb2mdb.exe %1
