add_library(MSCMPClient SHARED)

add_custom_target(
	GenerateNetworkMessages
	VERBATIM
	BYPRODUCTS ${CMAKE_CURRENT_SOURCE_DIR}/Network/NetMessages.generated.cs
	WORKING_DIRECTORY ${CMAKE_BINARY_DIR}/src/MSCMPMessages/$<CONFIG>
	COMMAND MSCMPMessages.exe ${CMAKE_CURRENT_SOURCE_DIR}/Network
	DEPENDS MSCMPMessages
	)

add_dependencies(MSCMPClient GenerateNetworkMessages)

target_sources(MSCMPClient
	PRIVATE
	${CMAKE_CURRENT_SOURCE_DIR}/Client.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Development/WorldDumper.cs
	${CMAKE_CURRENT_SOURCE_DIR}/DevTools.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Components/ObjectSyncComponent.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Components/ObjectSyncPlayerComponent.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Components/PickupableMetaDataComponent.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/EventHook.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/GameCallbacks.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/GameDoorsManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/GameVehicleDatabase.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/IGameObjectCollector.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/GamePickupableDatabase.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/GameWeatherManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/GameWorld.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Hooks/PlayMakerActionHooks.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/IObjectSubtype.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/ISyncedObject.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/ObjectSyncManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/AIVehicle.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/LightSwitchManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/Boat.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/PlayerVehicle.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/GarageDoor.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/GameDoor.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/GamePlayer.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/LightSwitch.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/PassengerSeat.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/Pickupable.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/PickupableTypes/BeerCase.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/PickupableTypes/PubFood.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/PickupableTypes/ShoppingBag.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Objects/PickupableTypes/Consumable.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/Places/Shop.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Game/TrafficManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Logger.cs
	${CMAKE_CURRENT_SOURCE_DIR}/MessagesList.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetMessageHandler.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetPickupable.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetStatistics.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/PlayerAnimManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/UI/Console.cs
	${CMAKE_CURRENT_SOURCE_DIR}/UI/Handlers/MessageBoxHandler.cs
	${CMAKE_CURRENT_SOURCE_DIR}/UI/MPGUI.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Math/QuaternionInterpolator.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Math/TransformInterpolator.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Math/Vector3Interpolator.cs
	${CMAKE_CURRENT_SOURCE_DIR}/MPController.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/INetMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetLocalPlayer.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetManager.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetMessages.generated.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetPlayer.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Network/NetWorld.cs
	${CMAKE_CURRENT_SOURCE_DIR}/PlayMakerUtils.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Properties/AssemblyInfo.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Utilities/HTMLWriter.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Utilities/IMGUIUtils.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Utils.cs
)

set_property(TARGET MSCMPClient PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v3.5")
set_property(TARGET MSCMPClient PROPERTY WIN32_EXECUTABLE TRUE)

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
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/Assembly-CSharp.dll" Assembly_CSharp_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/Assembly-CSharp.dll" Assembly_CSharp_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/Assembly-CSharp-firstpass.dll" Assembly_CSharp_firstpass_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/Assembly-UnityScript.dll" Assembly_UnityScript_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/Assembly-UnityScript-firstpass.dll" Assembly_UnityScript_firstpass_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/PlayMaker.dll" Assembly_PlayMaker_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/UnityEngine.dll" Assembly_UnityEngine_PATH )
file(TO_NATIVE_PATH "${MSCMP_GAME_ASSEMBLIES_PATH}/UnityEngine.UI.dll" Assembly_UnityEngine_UI_PATH )
file(TO_NATIVE_PATH "${CMAKE_SOURCE_DIR}/src/packages/TaskParallelLibrary.1.0.2856.0/lib/Net35/System.Threading.dll" TaskParallelLibrary_PATH)

set_target_properties(MSCMPClient
	PROPERTIES
	VS_DOTNET_REFERENCES ${ASSEMBLY_REFS}
	VS_DOTNET_REFERENCE_Assembly_CSharp ${Assembly_CSharp_PATH}
	VS_DOTNET_REFERENCE_Assembly_CSharp_firstpass ${Assembly_CSharp_firstpass_PATH}
	VS_DOTNET_REFERENCE_Assembly_UnityScript ${Assembly_UnityScript_PATH}
	VS_DOTNET_REFERENCE_Assembly_UnityScript_firstpass ${Assembly_UnityScript_firstpass_PATH}
	VS_DOTNET_REFERENCE_Assembly_PlayMaker ${Assembly_PlayMaker_PATH}
	VS_DOTNET_REFERENCE_Assembly_UnityEngine ${Assembly_UnityEngine_PATH}
	VS_DOTNET_REFERENCE_Assembly_UnityEngine_UI ${Assembly_UnityEngine_UI_PATH}
	VS_DOTNET_REFERENCE_TaskParralelLibrary ${TaskParallelLibrary_PATH}
)