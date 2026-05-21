# SpaceTraders.Generated.Model.Contract
Contract details.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **string** | ID of the contract. | 
**FactionSymbol** | **string** | The symbol of the faction that this contract is for. | 
**Type** | **string** | Type of contract. | 
**Terms** | [**ContractTerms**](ContractTerms.md) |  | 
**Accepted** | **bool** | Whether the contract has been accepted by the agent | [default to false]
**Fulfilled** | **bool** | Whether the contract has been fulfilled | [default to false]
**Expiration** | **DateTimeOffset** | Deprecated in favor of deadlineToAccept | 
**DeadlineToAccept** | **DateTimeOffset** | The time at which the contract is no longer available to be accepted | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

