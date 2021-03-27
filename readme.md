# My Summer Car Multiplayer Mod 

## How to work on the mod?

### Build with CMake OR with Visual Studio
----

### Build with CMake

#### 1. Configure build with CMake. Set path ot steam installation of My Summer Car with MSCMP_GAME_PATH option
`cmake -S . -B build -DMSCMP_GAME_PATH="PATH_TO_STEAM_INSTALLATION_OF_MY_SUMMER_CAR"`

#### 2. Build with CMake.
`cmake --build build --target [optional_target_name] -j thread_count`

----

### Build With Visual Studio

`setx /m MSCMP_GAME_PATH "PATH_TO_STEAM_INSTALLATION_OF_MY_SUMMER_CAR"`

#### 3. Launch Visual Studio 2019 solution.

Launch the mod solution placed in `src` folder.

#### 4. Generate network messages.

First of all compile and run `MSCMPMessages` app. It will generate network messages file network code requires to compile.

#### 5. Build rest of the projects.

Build `MSCMP`, `MSCMPInjector` and `MSCMPClient` projects.

----

### Prepare mutliplayer data asset bundle.

1. Download Unity 5.0.0f4 that is used by the mod files. You can find binaries [here](https://unity3d.com/get-unity/download/archive).
2. Open Unity and load the project that can be found in the `unity` folder in the top level folder of the repository.
3. From the unity menu bar select MSCMP > Build Asset Bundles.
4. Done!

### Play & develop!

That's all. Now you can go to the `bin\Debug` folder and launch mod via `MSCMP.exe` executable. (Or `bin/Release` depending which configuration you are using)



## License

For the project license check `LICENSE` file.

### Using

* pdb2mdb is licensed under the Microsoft Public License (Ms-PL).
* Mono.Cecil is licensed under the MIT/X11.
