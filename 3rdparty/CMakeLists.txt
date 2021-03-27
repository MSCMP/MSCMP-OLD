add_library(mono INTERFACE)

target_include_directories(mono 
	INTERFACE 
		${CMAKE_CURRENT_SOURCE_DIR}/mono-2.0/include
)

add_library(steamapi SHARED IMPORTED GLOBAL)

set_target_properties(steamapi
	PROPERTIES
		IMPORTED_LOCATION ${CMAKE_CURRENT_SOURCE_DIR}/steamapi/steam_api64.dll
		IMPORTED_IMPLIB ${CMAKE_CURRENT_SOURCE_DIR}/steamapi/steam_api64.lib
		INTERFACE_INCLUDE_DIRECTORIES ${CMAKE_CURRENT_SOURCE_DIR}/steamapi/include
	)