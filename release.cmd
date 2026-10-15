@echo off
rem Releases what is on main: release.cmd for the next patch, release.cmd minor, release.cmd major, or release.cmd v0.1.0.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0release.ps1" %*
