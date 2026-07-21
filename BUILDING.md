# gf Mod distribution build

This project builds a BepInEx plugin and a Windows installer.

```bat
build_release.bat "D:\games\steam\steamapps\common\Super Battle Golf"
```

The output is `Release\gfModInstaller.exe` and `gfMod.zip`.

The installer embeds `gfMod.dll` together with the BepInEx 5 bootstrap files from
the build game folder. It installs the plugin to `BepInEx\plugins\gfMod.dll` and
can therefore set up a clean game folder as well.
