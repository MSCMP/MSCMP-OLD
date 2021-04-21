# My Summer Car Multiplayer Mod 

## How to work on the mod?

### 1. Configure build with CMake. Set path ot steam installation of My Summer Car with MSC_GAME_PATH option
`cmake -S . -B build -DMSC_GAME_PATH="PATH_TO_STEAM_INSTALLATION_OF_MY_SUMMER_CAR" -DCMAKE_INSTALL_PREFIX="PATH_TO_INSTALL_BUILDED_DIST_OF_MOD"`

### 2. Build with CMake.
`cmake --build build --target [optional_target_name] --config BUILD_TYPE -j thread_count`

### 3. Prepare mutliplayer data asset bundle.

1. Download Unity 5.0.0f4 that is used by the mod files. You can find binaries [here](https://unity3d.com/get-unity/download/archive).
2. Open Unity and load the project that can be found in the `unity` folder in the top level folder of the repository.
3. From the unity menu bar select MSCMP > Build Asset Bundles.
4. Done!

### 4. Install with CMake
`cmake --install build --config BUILD_TYPE`

### Play & develop!

That's all. Now you can go to the folder and launch mod via `MSCMP.exe` executable.


## License

For the project license check `LICENSE` file.

### Using

* pdb2mdb is licensed under the Microsoft Public License (Ms-PL).
* Mono.Cecil is licensed under the MIT/X11.
