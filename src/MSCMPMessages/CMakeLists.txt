add_executable(MSCMPMessages)
target_sources(MSCMPMessages
	PRIVATE
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/AnimSyncMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Generator.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/AskForWorldStateMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/VehicleStateMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/DisconnectMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/EventHookSyncMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/FullWorldSyncMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/HandshakeMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/HeartbeatMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/HeartbeatResponseMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/LightSwitchMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/MessageIds.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/ObjectSyncMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/ObjectSyncRequestMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/ObjectSyncResponseMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/OpenDoorsMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/PickupableActivateMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/PickupableDestroyMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/PickupableSetPositionMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/PickupableSpawnMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/PlayerSyncMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/QuaternionMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/TransformMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/Vector3Message.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/VehicleSwitchMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/VehicleEnterMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/VehicleLeaveMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/WeatherUpdateMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Messages/WorldPeriodicalUpdateMessage.cs
	${CMAKE_CURRENT_SOURCE_DIR}/NetMessageDesc.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Optional.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Program.cs
	${CMAKE_CURRENT_SOURCE_DIR}/Properties/AssemblyInfo.cs
)

set_property(TARGET MSCMPMessages PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.7.2")
set_property(TARGET MSCMPMessages PROPERTY WIN32_EXECUTABLE TRUE)

set_property(TARGET MSCMPMessages PROPERTY VS_DOTNET_REFERENCES
    "Microsoft.CSharp"
    "System"
    "System.Core"
    "System.Data"
    "System.Data.DataSetExtensions"
    "System.Net.Http"
    "System.Xml"
    "System.Xml.Linq"
)