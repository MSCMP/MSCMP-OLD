add_custom_target(NugetRestore
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR}/src/MSCMPClient
    COMMAND nuget restore -PackagesDirectory ${CMAKE_SOURCE_DIR}/src/packages
)