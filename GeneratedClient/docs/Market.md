# SpaceTraders.Generated.Model.Market
Market details.

## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Symbol** | **string** | The symbol of the market. The symbol is the same as the waypoint where the market is located. | 
**Exports** | [**List&lt;TradeGood&gt;**](TradeGood.md) | The list of goods that are exported from this market. | 
**Imports** | [**List&lt;TradeGood&gt;**](TradeGood.md) | The list of goods that are sought as imports in this market. | 
**Exchange** | [**List&lt;TradeGood&gt;**](TradeGood.md) | The list of goods that are bought and sold between agents at this market. | 
**Transactions** | [**List&lt;MarketTransaction&gt;**](MarketTransaction.md) | The list of recent transactions at this market. Visible only when a ship is present at the market. | [optional] 
**TradeGoods** | [**List&lt;MarketTradeGood&gt;**](MarketTradeGood.md) | The list of goods that are traded at this market. Visible only when a ship is present at the market. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

