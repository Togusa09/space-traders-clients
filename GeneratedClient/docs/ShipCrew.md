# SpaceTraders.Generated.Model.ShipCrew
The ship's crew service and maintain the ship's systems and equipment.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Current** | **int** | The current number of crew members on the ship. | 
**Required** | **int** | The minimum number of crew members required to maintain the ship. | 
**Capacity** | **int** | The maximum number of crew members the ship can support. | 
**Rotation** | **string** | The rotation of crew shifts. A stricter shift improves the ship&#39;s performance. A more relaxed shift improves the crew&#39;s morale. | [default to RotationEnum.STRICT]
**Morale** | **int** | A rough measure of the crew&#39;s morale. A higher morale means the crew is happier and more productive. A lower morale means the ship is more prone to accidents. | 
**Wages** | **int** | The amount of credits per crew member paid per hour. Wages are paid when a ship docks at a civilized waypoint. | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

