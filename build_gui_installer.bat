@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "GAME_ROOT=%~1"
if not defined GAME_ROOT if defined SUPER_BATTLE_GOLF_DIR set "GAME_ROOT=%SUPER_BATTLE_GOLF_DIR%"
if not defined GAME_ROOT (
    echo [ERROR] Pass the Super Battle Golf folder as the first argument.
    exit /b 1
)
for %%I in ("%GAME_ROOT%") do set "GAME_ROOT=%%~fI"

if not exist "%GAME_ROOT%\Super Battle Golf.exe" (
    echo [ERROR] Invalid game folder: %GAME_ROOT%
    exit /b 1
)
if not exist "%GAME_ROOT%\BepInEx\core\BepInEx.dll" (
    echo [ERROR] BepInEx is required for the gf Mod installer.
    exit /b 1
)
for %%F in ("%GAME_ROOT%\winhttp.dll" "%GAME_ROOT%\doorstop_config.ini" "%GAME_ROOT%\.doorstop_version") do if not exist "%%~F" (
    echo [ERROR] BepInEx bootstrap file is missing: %%~F
    exit /b 1
)
if not exist "Mods\gfMod.BepInEx.dll" (
    echo [ERROR] Build the BepInEx plugin first with compile_mod.bat.
    exit /b 1
)

set "CSC_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC_PATH%" set "CSC_PATH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC_PATH%" (
    echo [ERROR] C# compiler not found: %CSC_PATH%
    exit /b 1
)

if exist "Installer\_PayloadStage" rmdir /s /q "Installer\_PayloadStage"
if exist "Installer\PayloadBundle.zip" del /q "Installer\PayloadBundle.zip"
mkdir "Installer\_PayloadStage\BepInEx\plugins"
mkdir "Installer\_PayloadStage\BepInEx\core"
mkdir "Installer\_PayloadStage\BepInEx\patchers"
copy /y "Mods\gfMod.BepInEx.dll" "Installer\_PayloadStage\BepInEx\plugins\gfMod.dll" >nul
copy /y "%GAME_ROOT%\winhttp.dll" "Installer\_PayloadStage\winhttp.dll" >nul
copy /y "%GAME_ROOT%\doorstop_config.ini" "Installer\_PayloadStage\doorstop_config.ini" >nul
copy /y "%GAME_ROOT%\.doorstop_version" "Installer\_PayloadStage\.doorstop_version" >nul
xcopy /E /I /Q /Y "%GAME_ROOT%\BepInEx\core" "Installer\_PayloadStage\BepInEx\core" >nul
xcopy /E /I /Q /Y "%GAME_ROOT%\BepInEx\patchers" "Installer\_PayloadStage\BepInEx\patchers" >nul

powershell -NoProfile -Command "Compress-Archive -Path 'Installer\_PayloadStage\*' -DestinationPath 'Installer\PayloadBundle.zip' -Force"
if errorlevel 1 (
    echo [ERROR] Failed to create installer payload.
    exit /b 1
)

echo [1/2] Building gfModInstaller.exe...
"%CSC_PATH%" /nologo /target:winexe /optimize+ /debug:pdbonly /pdb:"Installer\gfModInstaller.pdb" /out:"Installer\gfModInstaller.exe" /resource:"Installer\PayloadBundle.zip",gfMod.PayloadBundle.zip /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll "Installer\BirdieModInstaller.cs"
if errorlevel 1 exit /b 1

echo [2/2] Building gfModUninstaller.exe...
"%CSC_PATH%" /nologo /target:winexe /optimize+ /debug:pdbonly /pdb:"Installer\gfModUninstaller.pdb" /out:"Installer\gfModUninstaller.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "Installer\BirdieModUninstaller.cs"
if errorlevel 1 exit /b 1

if not exist "Release" mkdir "Release"
copy /y "Installer\gfModInstaller.exe" "Release\gfModInstaller.exe" >nul
copy /y "Installer\gfModUninstaller.exe" "Release\gfModUninstaller.exe" >nul
copy /y "Mods\gfMod.BepInEx.dll" "Release\gfMod.dll" >nul
copy /y "Mods\gfMod.BepInEx.pdb" "Release\gfMod.pdb" >nul
echo [OK] Release\gfModInstaller.exe
exit /b 0
