add_executable(MSCMP WIN32)
target_sources(MSCMP 
	PRIVATE
		${CMAKE_CURRENT_SOURCE_DIR}/Main.cpp
)

target_link_libraries(MSCMP 
	PRIVATE
	mono
	steamapi
	)