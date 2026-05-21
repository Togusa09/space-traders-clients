# SpaceTraders.Generated.Model.SystemWaypoint
Waypoint details.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Symbol** | **string** | The symbol of the waypoint. | 
**Type** | **WaypointType** |  | 
**X** | **int** | Relative position of the waypoint on the system&#39;s x axis. This is not an absolute position in the universe. | 
**Y** | **int** | Relative position of the waypoint on the system&#39;s y axis. This is not an absolute position in the universe. | 
**Orbitals** | [**List&lt;WaypointOrbital&gt;**](WaypointOrbital.md) | Waypoints that orbit this waypoint. | 
**Orbits** | **string** | The symbol of the parent waypoint, if this waypoint is in orbit around another waypoint. Otherwise this value is undefined. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

