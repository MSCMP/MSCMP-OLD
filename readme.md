# My Summer Car Multiplayer Mod

## How to work on the mod?

### 1. Clone repository.

First of all you have to clone repository.

1. Install GIT client (for example: https://git-scm.com)
2. Go to the folder where you want to download mod source code, click right mouse button and start Git Bash.
3. Type `git clone git@github.com:RootKiller/MSCMP.git`
4. Repository will now be cloned to your disk.

### 2. Setup workspace.

Before you start working on the mod make sure you properly setup your workspace. To do that run `setup.bat` file it will setup everything for you.

### 3. Launch Visual Studio 2015 solution.

Launch the mod solution placed in `src` folder.

### 4. Generate network messages.

First of all compile and run `MSCMPMessages` app. It will generate network messages file network code requires to compile.

### 5. Build rest of the projects.

Build `MSCMP`, `MSCMPInjector` and `MSCMPClient` projects.

### 6. Prepare mutliplayer data asset bundle.

1. Download Unity 5.0.0f4 that is used by the mod files. You can find binaries [here](https://unity3d.com/get-unity/download/archive).
2. Open Unity and load the project that can be found in the `unity` folder in the top level folder of the repository.
3. From the unity menu bar select MSCMP > Build Asset Bundles.
4. Done!

### 7. Play & develop!

That's all. Now you can go to the `bin\Debug` folder and launch mod via `MSCMP.exe` executable. (Or `bin/Release` depending which configuration you are using)

## License

The project source code has been published under GPL v3 license. Check `LICENSE` file for details.
