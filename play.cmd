@echo off
rem Builds the C# and runs the game. Arguments go to the game: play.cmd --host, play.cmd --join=192.168.1.5
setlocal
rem No build servers left behind once the build is done: they idle in the task manager otherwise.
set MSBUILDDISABLENODEREUSE=1
set DOTNET_CLI_USE_MSBUILD_SERVER=0
set UseSharedCompilation=false
set GODOT=%USERPROFILE%\Tools\godot-4.7.2\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe
if not "%STILLWAKE_GODOT%"=="" set GODOT=%STILLWAKE_GODOT%
set GODOT_CONSOLE=%GODOT:.exe=_console.exe%
if not exist "%GODOT_CONSOLE%" set GODOT_CONSOLE=%GODOT%
cd /d "%~dp0"
"%ProgramFiles%\dotnet\dotnet.exe" build Stillwake.sln -v quiet -nologo || exit /b 1
rem Godot imports art added since the last run first: a game started without it fails to load that art.
"%GODOT_CONSOLE%" --headless --path game --import >nul 2>&1
start "" "%GODOT%" --path game -- %*
