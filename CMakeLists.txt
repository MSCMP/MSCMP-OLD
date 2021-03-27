cmake_minimum_required(VERSION 3.19)

project(MySummerCarMP LANGUAGES CSharp CXX ASM_MASM)

include(CSharpUtilities)

option(MSCMP_GAME_PATH "Path to Installed My Summer Car" OFF)

if(NOT MSCMP_GAME_PATH)
	message(FATAL_ERROR "MSCMP_GAME_PATH Must be set")
else()
	message("MSCMP_GAME_PATH is ${MSCMP_GAME_PATH}")
endif()


add_subdirectory(3rdparty)

add_subdirectory(src/MSCMP)
add_subdirectory(src/MSCMPInjector)

add_subdirectory(src/MSCMPMessages)
add_subdirectory(src/MSCMPClient)