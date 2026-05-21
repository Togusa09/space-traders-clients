# SpaceTraders.Generated.Model.Shipyard
Shipyard details.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Symbol** | **string** | The symbol of the shipyard. The symbol is the same as the waypoint where the shipyard is located. | 
**ShipTypes** | [**List&lt;ShipyardShipTypesInner&gt;**](ShipyardShipTypesInner.md) | The list of ship types available for purchase at this shipyard. | 
**Transactions** | [**List&lt;ShipyardTransaction&gt;**](ShipyardTransaction.md) | The list of recent transactions at this shipyard. | [optional] 
**Ships** | [**List&lt;ShipyardShip&gt;**](ShipyardShip.md) | The ships that are currently available for purchase at the shipyard. | [optional] 
**ModificationsFee** | **int** | The fee to modify a ship at this shipyard. This includes installing or removing modules and mounts on a ship. In the case of mounts, the fee is a flat rate per mount. In the case of modules, the fee is per slot the module occupies. | 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

