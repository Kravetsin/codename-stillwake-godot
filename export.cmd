@echo off
rem Exports the Windows build to build\windows\Stillwake.exe and zips it as build\Stillwake-windows.zip.
setlocal
set GODOT=%USERPROFILE%\Tools\godot-4.7.2\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe
if not "%STILLWAKE_GODOT%"=="" set GODOT=%STILLWAKE_GODOT%
cd /d "%~dp0"
"%ProgramFiles%\dotnet\dotnet.exe" build Stillwake.sln -v quiet -nologo || exit /b 1
if exist build\windows rmdir /s /q build\windows
mkdir build\windows
"%GODOT%" --headless --path game --import
"%GODOT%" --headless --path game --export-release "Windows Desktop" ..\build\windows\Stillwake.exe || exit /b 1
rem The co-op launchers: host or join through the playtest server's relay.
copy /y deploy\launchers\host.cmd build\windows\ >nul
copy /y deploy\launchers\join.cmd build\windows\ >nul
powershell -NoProfile -Command "Compress-Archive -Path build\windows\* -DestinationPath build\Stillwake-windows.zip -Force"
echo Built build\windows\Stillwake.exe and build\Stillwake-windows.zip
