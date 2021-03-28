cmake_minimum_required(VERSION 3.19)

project(MySummerCarMP LANGUAGES CSharp CXX ASM_MASM)

include(CSharpUtilities)
include(cmake/NugetRestore.cmake)

option(MSCMP_GAME_PATH "Path to Installed My Summer Car" OFF)

if(NOT MSCMP_GAME_PATH)
	message(FATAL_ERROR "MSCMP_GAME_PATH Must be set")
else()
	message("MSCMP_GAME_PATH is ${MSCMP_GAME_PATH}")
endif()


add_subdirectory(3rdparty)

add_subdirectory(src/MSCMP)
add_subdirectory(src/MSCMPInjector)

set(CMAKE_CSharp_FLAGS "/langversion:latest /platform:anycpu")

add_subdirectory(src/MSCMPMessages)
add_subdirectory(src/MSCMPClient)

add_dependencies(MSCMPClient NugetRestore)

install(
	TARGETS MSCMP MSCMPInjector MSCMPClient
	DESTINATION ${CMAKE_INSTALL_PREFIX}
)

get_target_property(STEAMAPI_DLL steamapi IMPORTED_LOCATION)
set(STEAM_APPID ${CMAKE_SOURCE_DIR}/data/steam_appid.txt)

install(
	FILES 
		${STEAMAPI_DLL}
		${STEAM_APPID}
	DESTINATION
		${CMAKE_INSTALL_PREFIX}
)

install(
	FILES
		${CMAKE_SOURCE_DIR}/data/data
		${CMAKE_SOURCE_DIR}/data/data.manifest
		${CMAKE_SOURCE_DIR}/data/mpdata
		${CMAKE_SOURCE_DIR}/data/mpdata.manifest
	DESTINATION
		${CMAKE_INSTALL_PREFIX}/data
)