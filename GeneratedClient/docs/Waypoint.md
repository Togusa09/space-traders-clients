# SpaceTraders.Generated.Model.Waypoint
A waypoint is a location that ships can travel to such as a Planet, Moon or Space Station.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Symbol** | **string** | The symbol of the waypoint. | 
**Type** | **WaypointType** |  | 
**SystemSymbol** | **string** | The symbol of the system. | 
**X** | **int** | Relative position of the waypoint on the system&#39;s x axis. This is not an absolute position in the universe. | 
**Y** | **int** | Relative position of the waypoint on the system&#39;s y axis. This is not an absolute position in the universe. | 
**Orbitals** | [**List&lt;WaypointOrbital&gt;**](WaypointOrbital.md) | Waypoints that orbit this waypoint. | 
**Orbits** | **string** | The symbol of the parent waypoint, if this waypoint is in orbit around another waypoint. Otherwise this value is undefined. | [optional] 
**Faction** | [**WaypointFaction**](WaypointFaction.md) |  | [optional] 
**Traits** | [**List&lt;WaypointTrait&gt;**](WaypointTrait.md) | The traits of the waypoint. | 
**Modifiers** | [**List&lt;WaypointModifier&gt;**](WaypointModifier.md) | The modifiers of the waypoint. | [optional] 
**Chart** | [**Chart**](Chart.md) |  | [optional] 
**IsUnderConstruction** | **bool** | True if the waypoint is under construction. | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

