add_library(MSCMPInjector SHARED)

target_sources(MSCMPInjector 
	PRIVATE
		${CMAKE_CURRENT_SOURCE_DIR}/HooksASM.asm
		${CMAKE_CURRENT_SOURCE_DIR}/Main.cpp
		${CMAKE_CURRENT_SOURCE_DIR}/Hooks.cpp
		${CMAKE_CURRENT_SOURCE_DIR}/MemFunctions.cpp
		${CMAKE_CURRENT_SOURCE_DIR}/MonoLoader.cpp
	PUBLIC
		${CMAKE_CURRENT_SOURCE_DIR}/MemFunctions.h
		${CMAKE_CURRENT_SOURCE_DIR}/MonoLoader.h
)

target_link_libraries(MSCMPInjector 
	PUBLIC
	mono
	steamapi
)