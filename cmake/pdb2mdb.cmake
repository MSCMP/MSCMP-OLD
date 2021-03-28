set(pdb2mdb_PATH ${CMAKE_SOURCE_DIR}/src/packages/Mono.Unofficial.pdb2mdb.4.2.3.4/tools/pdb2mdb.exe)
set(MSCMPClient_MDB_DEBUGINFO_PATH_ ${CMAKE_BINARY_DIR}/src/MSCMPClient/$<CONFIG>/MSCMPClient.dll.mdb)
set(MSCMPClient_MDB_DEBUGINFO_PATH $<$<CONFIG:Debug>:${MSCMPClient_MDB_DEBUGINFO_PATH_}>)
add_custom_target(PDB2MDB_MSCMPClient
    WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/src/MSCMPClient/$<CONFIG>
		BYPRODUCTS MSCMPClient_MDB_DEBUGINFO_PATH
		COMMAND $<$<CONFIG:Debug>:${pdb2mdb_PATH},echo> MSCMPClient.dll
)