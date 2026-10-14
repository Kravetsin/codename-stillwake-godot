@echo off
rem Builds the C# and runs the game. Arguments go to the game: play.cmd --host, play.cmd --join=192.168.1.5
setlocal
set GODOT=%USERPROFILE%\Tools\godot-4.7.2\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe
if not "%STILLWAKE_GODOT%"=="" set GODOT=%STILLWAKE_GODOT%
cd /d "%~dp0"
"%ProgramFiles%\dotnet\dotnet.exe" build Stillwake.sln -v quiet -nologo || exit /b 1
start "" "%GODOT%" --path game -- %*
