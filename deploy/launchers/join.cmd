@echo off
rem Joins a friend's co-op session on the playtest server, by the name they hosted it as.
setlocal
set /p NAME=Session name: 
if "%STILLWAKE_PASSWORD%"=="" set /p STILLWAKE_PASSWORD=Playtest password: 
start "" "%~dp0Stillwake.exe" -- --relay --join=%NAME%
