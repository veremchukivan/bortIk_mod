@echo off
setlocal EnableExtensions EnableDelayedExpansion
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
    echo [ERROR] BepInEx is required to build gf Mod.
    exit /b 1
)

set "CSC_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
if not exist "%CSC_PATH%" set "CSC_PATH=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC_PATH%" (
    echo [ERROR] C# compiler not found: %CSC_PATH%
    exit /b 1
)

if not exist "Mods" mkdir "Mods"
set "MANAGED=%GAME_ROOT%\Super Battle Golf_Data\Managed"
set "RSP_FILE=Mods\gfMod.BepInEx.rsp"

> "%RSP_FILE%" echo /nologo
>> "%RSP_FILE%" echo /target:library
>> "%RSP_FILE%" echo /langversion:latest
>> "%RSP_FILE%" echo /optimize+
>> "%RSP_FILE%" echo /debug:pdbonly
>> "%RSP_FILE%" echo /define:BEPINEX
>> "%RSP_FILE%" echo /out:"Mods\gfMod.BepInEx.dll"
>> "%RSP_FILE%" echo /pdb:"Mods\gfMod.BepInEx.pdb"
>> "%RSP_FILE%" echo /reference:"%GAME_ROOT%\BepInEx\core\BepInEx.dll"

for %%R in (
    UnityEngine.dll
    UnityEngine.CoreModule.dll
    UnityEngine.UIModule.dll
    UnityEngine.UI.dll
    Unity.InputSystem.dll
    Unity.TextMeshPro.dll
    UnityEngine.PhysicsModule.dll
    UnityEngine.UIElementsModule.dll
    UnityEngine.IMGUIModule.dll
    UnityEngine.TextRenderingModule.dll
    UnityEngine.ParticleSystemModule.dll
    UnityEngine.AudioModule.dll
    UnityEngine.UnityWebRequestModule.dll
    UnityEngine.UnityWebRequestAudioModule.dll
    netstandard.dll
) do >> "%RSP_FILE%" echo /reference:"%MANAGED%\%%R"

for /R "Source" %%F in (*.cs) do if /I not "%%~nxF"=="gf.MelonEntry.cs" >> "%RSP_FILE%" echo "%%F"

echo [1/2] Building BepInEx plugin...
"%CSC_PATH%" @"%RSP_FILE%"
if errorlevel 1 (
    echo [ERROR] BepInEx build failed.
    exit /b 1
)

set "PLUGIN_DIR=%GAME_ROOT%\BepInEx\plugins"
if not exist "%PLUGIN_DIR%" mkdir "%PLUGIN_DIR%"
copy /y "Mods\gfMod.BepInEx.dll" "%PLUGIN_DIR%\gfMod.dll" >nul
if errorlevel 1 (
    echo [ERROR] Could not deploy gfMod.dll to BepInEx plugins.
    exit /b 1
)

echo [2/2] Done: %PLUGIN_DIR%\gfMod.dll
exit /b 0
