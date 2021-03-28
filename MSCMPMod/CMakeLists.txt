cmake_minimum_required(VERSION 3.19)

project(MySummerCarMPMod LANGUAGES CSharp)

include(CSharpUtilities)

option(MSCMP_GAME_PATH "Path to Installed My Summer Car" OFF)

if(NOT MSCMP_GAME_PATH)
	message(FATAL_ERROR "MSCMP_GAME_PATH Must be set")
else()
	message("MSCMP_GAME_PATH is ${MSCMP_GAME_PATH}")
endif()

add_library(MSCMPMod SHARED)

target_sources(MSCMPMod
	PRIVATE
		${CMAKE_CURRENT_SOURCE_DIR}/Command.cs
		${CMAKE_CURRENT_SOURCE_DIR}/Mod.cs
)

set_property(TARGET MSCMPMod PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v3.5")

set(ASSEMBLY_REFS 
	"System"
	"System.Core"
	"System.Xml.Linq"
	"System.Data.DataSetExtensions"
	"System.Data"
	"System.Collections.Concurrent"
	"System.Threading.Tasks"
	"System.Threading"
	"System.Xml"
)

file(TO_NATIVE_PATH "${MSCMP_GAME_PATH}/mysummercar_Data/Managed" MSCMP_GAME_ASSEMBLIES_PATH)
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/MSCLoader.dll" MSCLoader_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/UnityEngine.dll" Assembly_UnityEngine_PATH )


set_target_properties(MSCMPMod
	PROPERTIES
	VS_DOTNET_REFERENCES ${ASSEMBLY_REFS}
	VS_DOTNET_REFERENCE_MSCLoader ${MSCLoader_PATH}
	VS_DOTNET_REFERENCE_Assembly_UnityEngine ${Assembly_UnityEngine_PATH}
)

install(
	TARGETS
		MSCMPMod
	DESTINATION
		${MSCMP_GAME_ASSEMBLIES_PATH}
)