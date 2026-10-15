@echo off
rem Hosts a co-op session on the playtest server: friends join it by its name with join.cmd.
setlocal
set /p NAME=Session name (letters, digits, dashes): 
if "%STILLWAKE_PASSWORD%"=="" set /p STILLWAKE_PASSWORD=Playtest password: 
start "" "%~dp0Stillwake.exe" -- --relay --host=%NAME%
