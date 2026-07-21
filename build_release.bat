@echo off
setlocal EnableExtensions
cd /d "%~dp0"

call compile_mod.bat %*
if errorlevel 1 exit /b 1
call build_gui_installer.bat %*
if errorlevel 1 exit /b 1

tar -a -cf "gfMod.new.zip" Installer Mods Source Release compile_mod.bat build_gui_installer.bat build_release.bat BUILDING.md
if errorlevel 1 (
    echo [ERROR] Failed to create gfMod.zip.
    exit /b 1
)
move /y "gfMod.new.zip" "gfMod.zip" >nul
echo [OK] gfMod.zip created.
exit /b 0
