cmake_minimum_required(VERSION 3.19)

project(MySummerCarMP LANGUAGES CSharp CXX ASM_MASM)

include(CSharpUtilities)
include(cmake/NugetRestore.cmake)
include(cmake/pdb2mdb.cmake)

option(MSC_GAME_PATH "Path to Installed My Summer Car" OFF)

if(NOT MSC_GAME_PATH)
	message(FATAL_ERROR "MSC_GAME_PATH Must be set")
else()
	message("MSC_GAME_PATH is ${MSC_GAME_PATH}")
endif()


add_subdirectory(3rdparty)


set(CMAKE_CSharp_FLAGS "/langversion:latest /platform:anycpu")

add_subdirectory(src/MSCMPMessages)
add_subdirectory(src/MSCMPMod)

add_dependencies(MPMod NugetRestore)
add_dependencies(PDB2MDB_MPMOD MPMod)

install(
	TARGETS MPMod
	ARCHIVE DESTINATION ${CMAKE_INSTALL_PREFIX}/References
	RUNTIME DESTINATION ${CMAKE_INSTALL_PREFIX}
)

install(
	FILES 
		${MSCMPMod_MDB_DEBUGINFO_PATH}
		DESTINATION ${CMAKE_INSTALL_PREFIX}
)

install(
	FILES
		${CMAKE_SOURCE_DIR}/data/data
		${CMAKE_SOURCE_DIR}/data/data.manifest
		${CMAKE_SOURCE_DIR}/data/mpdata
		${CMAKE_SOURCE_DIR}/data/mpdata.manifest
	DESTINATION
		${CMAKE_INSTALL_PREFIX}/Assets/MPMod
)