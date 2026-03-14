Powered by [05126619z/ScavTemplate](https://github.com/05126619z/ScavTemplate).

[中文指南](README_ZH.md)

# BlackMossTemplate

A template for developing mods for `Casualties Unknown`.

# How to use?
_This guide is for JetBrains Rider. I don't like Visual Studio, so I won't write a guide for it._

**Maybe Visual Studio can do this too, but I don't know how.**

1. Clone [this template](https://github.com/new?template_name=BlackMossTemplate).
2. Get the game's DLL files:
   1. Right-click on `Dependencies`.
   2. Select `Reference...`, then `Add From...`.
   3. Go to your game directory (e.g., `E:/CasualtiesUnknownDemo/CasualtiesUnknown_Data/Managed`).
   4. Select all .dll files.
3. Rename the `BlackMossTemplate` folders to your mod name.
4. Replace `BlackMossTemplate` in the following files with your mod name:
   1. [Plugin.cs](Plugin.cs)
   2. [BlackMossTemplate.csproj](BlackMossTemplate.csproj)
5. Build the project to test if it runs. If not, repeat steps 2–4.
6. After a successful run, replace the following content in [Plugin.cs](Plugin.cs):
   1. Change `namespace BlackMossTemplate` to your namespace (it should match your mod name and cannot contain spaces).
   2. Replace `blackmoss.template` with your GUID in the format `yourname.modname`. ___Case and underscores are supported, but lowercase without underscores is recommended.___
   3. Replace `Black_Moss Template` with your mod name.
   4. Fill in the version `0.0.0` as you like—`114514.1919.810` is also acceptable.
   5. The content of the [_harmony](file://F:\CUProject\BlackMossTemplate\Plugin.cs#L10-L10) constant should match your GUID.
   6. `Logger.LogInfo("Here's Black Moss!");` can be anything you want ~~(if someone complains about random log spam, it's not my fault)~~.

# About StartGame.ps1
You should see a [StartGame.ps1](StartGame.ps1) file, which copies the compiled DLL file to the BepInEx plugin directory under the game directory.
You can run it directly in cmd/PowerShell by adding the game directory and mod namespace, then starting it.

Of course, you can also use Rider/Visual Studio to run it, which is more convenient for development.

## Rider:

## Rider:

Right-click [StartGame.ps1](StartGame.ps1) and select `Run 'StartGame.ps1'`. The run window will show a bunch of errors at this point. Then click the [StartGame.ps1](StartGame.ps1) button that just appeared next to the build button in the top-right corner of the Rider editor, select `Edit Configurations...`, fill in `Script arguments:` with your game directory and mod namespace, set `Command parameters` to `-ExecutionPolicy Bypass`, click the plus sign next to `Before launch`, choose `Build Solution`, and confirm.

After that, every time you press the green triangle button next to the build button, the mod's DLL file will automatically be copied to the BepInEx plugin directory under the game directory, the game will start automatically, and you can view the BepInEx runtime logs in the run window.

## Visual Studio:
IDK, There's no "Run" button after right-clicking. I don't use VS and don't feel like figuring it out. You figure it out yourself. :P
