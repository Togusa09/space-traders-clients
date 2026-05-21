# SpaceTraders.Generated.Api.FactionsApi

All URIs are relative to *https://api.spacetraders.io/v2*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetFaction**](FactionsApi.md#getfaction) | **GET** /factions/{factionSymbol} | Get Faction |
| [**GetFactions**](FactionsApi.md#getfactions) | **GET** /factions | List Factions |

<a id="getfaction"></a>
# **GetFaction**
> GetFaction200Response GetFaction (string factionSymbol)

Get Faction

View the details of a faction.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetFactionExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AccountToken
            config.AccessToken = "YOUR_BEARER_TOKEN";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FactionsApi(config);
            var factionSymbol = COSMIC;  // string | The faction symbol

            try
            {
                // Get Faction
                GetFaction200Response result = apiInstance.GetFaction(factionSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FactionsApi.GetFaction: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetFactionWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Faction
    ApiResponse<GetFaction200Response> response = apiInstance.GetFactionWithHttpInfo(factionSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FactionsApi.GetFactionWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **factionSymbol** | **string** | The faction symbol |  |

### Return type

[**GetFaction200Response**](GetFaction200Response.md)

### Authorization

[AccountToken](../README.md#AccountToken), [AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fetched a faction. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getfactions"></a>
# **GetFactions**
> GetFactions200Response GetFactions (int? page = null, int? limit = null)

List Factions

Return a paginated list of all the factions in the game.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetFactionsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FactionsApi(config);
            var page = 1;  // int? | What entry offset to request (optional)  (default to 1)
            var limit = 10;  // int? | How many entries to return per page (optional)  (default to 10)

            try
            {
                // List Factions
                GetFactions200Response result = apiInstance.GetFactions(page, limit);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FactionsApi.GetFactions: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetFactionsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List Factions
    ApiResponse<GetFactions200Response> response = apiInstance.GetFactionsWithHttpInfo(page, limit);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FactionsApi.GetFactionsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **page** | **int?** | What entry offset to request | [optional] [default to 1] |
| **limit** | **int?** | How many entries to return per page | [optional] [default to 10] |

### Return type

[**GetFactions200Response**](GetFactions200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fetched factions. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

