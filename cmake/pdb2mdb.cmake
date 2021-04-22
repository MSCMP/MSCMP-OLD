set(pdb2mdb_PATH ${CMAKE_SOURCE_DIR}/src/packages/Mono.Unofficial.pdb2mdb.4.2.3.4/tools/pdb2mdb.exe)
set(MSCMPMod_MDB_DEBUGINFO_PATH_ ${CMAKE_BINARY_DIR}/src/MSCMPMod/$<CONFIG>/MPMod.dll.mdb)
set(MSCMPMod_MDB_DEBUGINFO_PATH $<$<CONFIG:Debug>:${MSCMPMod_MDB_DEBUGINFO_PATH_}>)
add_custom_target(PDB2MDB_MPMOD ALL
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/src/MSCMPMod/$<CONFIG>
		BYPRODUCTS MSCMPMod_MDB_DEBUGINFO_PATH
		COMMAND  powershell -Command $<$<CONFIG:Debug>:"${pdb2mdb_PATH} MPMod.dll">
)