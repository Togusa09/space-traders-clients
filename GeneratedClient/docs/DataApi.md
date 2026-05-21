# SpaceTraders.Generated.Api.DataApi

All URIs are relative to *https://api.spacetraders.io/v2*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**GetSupplyChain**](DataApi.md#getsupplychain) | **GET** /market/supply-chain | Get Supply Chain |

<a id="getsupplychain"></a>
# **GetSupplyChain**
> GetSupplyChain200Response GetSupplyChain ()

Get Supply Chain

Describes which import and exports map to each other.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetSupplyChainExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new DataApi(config);

            try
            {
                // Get Supply Chain
                GetSupplyChain200Response result = apiInstance.GetSupplyChain();
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling DataApi.GetSupplyChain: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetSupplyChainWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Supply Chain
    ApiResponse<GetSupplyChain200Response> response = apiInstance.GetSupplyChainWithHttpInfo();
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling DataApi.GetSupplyChainWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters
This endpoint does not need any parameter.
### Return type

[**GetSupplyChain200Response**](GetSupplyChain200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully retrieved the supply chain information |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

