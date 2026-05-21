# SpaceTraders.Generated.Api.ContractsApi

All URIs are relative to *https://api.spacetraders.io/v2*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**AcceptContract**](ContractsApi.md#acceptcontract) | **POST** /my/contracts/{contractId}/accept | Accept Contract |
| [**DeliverContract**](ContractsApi.md#delivercontract) | **POST** /my/contracts/{contractId}/deliver | Deliver Cargo to Contract |
| [**FulfillContract**](ContractsApi.md#fulfillcontract) | **POST** /my/contracts/{contractId}/fulfill | Fulfill Contract |
| [**GetContract**](ContractsApi.md#getcontract) | **GET** /my/contracts/{contractId} | Get Contract |
| [**GetContracts**](ContractsApi.md#getcontracts) | **GET** /my/contracts | List Contracts |

<a id="acceptcontract"></a>
# **AcceptContract**
> AcceptContract200Response AcceptContract (string contractId)

Accept Contract

Accept a contract by ID.   You can only accept contracts that were offered to you, were not accepted yet, and whose deadlines has not passed yet.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class AcceptContractExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new ContractsApi(config);
            var contractId = "contractId_example";  // string | The contract ID to accept.

            try
            {
                // Accept Contract
                AcceptContract200Response result = apiInstance.AcceptContract(contractId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ContractsApi.AcceptContract: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the AcceptContractWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Accept Contract
    ApiResponse<AcceptContract200Response> response = apiInstance.AcceptContractWithHttpInfo(contractId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ContractsApi.AcceptContractWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractId** | **string** | The contract ID to accept. |  |

### Return type

[**AcceptContract200Response**](AcceptContract200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully accepted contract. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="delivercontract"></a>
# **DeliverContract**
> DeliverContract200Response DeliverContract (string contractId, DeliverContractRequest deliverContractRequest = null)

Deliver Cargo to Contract

Deliver cargo to a contract.  In order to use this API, a ship must be at the delivery location (denoted in the delivery terms as `destinationSymbol` of a contract) and must have a number of units of a good required by this contract in its cargo.  Cargo that was delivered will be removed from the ship's cargo.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class DeliverContractExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new ContractsApi(config);
            var contractId = "contractId_example";  // string | The ID of the contract.
            var deliverContractRequest = new DeliverContractRequest(); // DeliverContractRequest |  (optional) 

            try
            {
                // Deliver Cargo to Contract
                DeliverContract200Response result = apiInstance.DeliverContract(contractId, deliverContractRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ContractsApi.DeliverContract: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DeliverContractWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Deliver Cargo to Contract
    ApiResponse<DeliverContract200Response> response = apiInstance.DeliverContractWithHttpInfo(contractId, deliverContractRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ContractsApi.DeliverContractWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractId** | **string** | The ID of the contract. |  |
| **deliverContractRequest** | [**DeliverContractRequest**](DeliverContractRequest.md) |  | [optional]  |

### Return type

[**DeliverContract200Response**](DeliverContract200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully delivered cargo to contract. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="fulfillcontract"></a>
# **FulfillContract**
> FulfillContract200Response FulfillContract (string contractId)

Fulfill Contract

Fulfill a contract. Can only be used on contracts that have all of their delivery terms fulfilled.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class FulfillContractExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new ContractsApi(config);
            var contractId = "contractId_example";  // string | The ID of the contract to fulfill.

            try
            {
                // Fulfill Contract
                FulfillContract200Response result = apiInstance.FulfillContract(contractId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ContractsApi.FulfillContract: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the FulfillContractWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Fulfill Contract
    ApiResponse<FulfillContract200Response> response = apiInstance.FulfillContractWithHttpInfo(contractId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ContractsApi.FulfillContractWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractId** | **string** | The ID of the contract to fulfill. |  |

### Return type

[**FulfillContract200Response**](FulfillContract200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fulfilled a contract. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getcontract"></a>
# **GetContract**
> GetContract200Response GetContract (string contractId)

Get Contract

Get the details of a contract by ID.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetContractExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new ContractsApi(config);
            var contractId = "contractId_example";  // string | The contract ID

            try
            {
                // Get Contract
                GetContract200Response result = apiInstance.GetContract(contractId);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ContractsApi.GetContract: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetContractWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Contract
    ApiResponse<GetContract200Response> response = apiInstance.GetContractWithHttpInfo(contractId);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ContractsApi.GetContractWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **contractId** | **string** | The contract ID |  |

### Return type

[**GetContract200Response**](GetContract200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fetched contract. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getcontracts"></a>
# **GetContracts**
> GetContracts200Response GetContracts (int? page = null, int? limit = null)

List Contracts

Return a paginated list of all your contracts.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetContractsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new ContractsApi(config);
            var page = 1;  // int? | What entry offset to request (optional)  (default to 1)
            var limit = 10;  // int? | How many entries to return per page (optional)  (default to 10)

            try
            {
                // List Contracts
                GetContracts200Response result = apiInstance.GetContracts(page, limit);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling ContractsApi.GetContracts: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetContractsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List Contracts
    ApiResponse<GetContracts200Response> response = apiInstance.GetContractsWithHttpInfo(page, limit);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling ContractsApi.GetContractsWithHttpInfo: " + e.Message);
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

[**GetContracts200Response**](GetContracts200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully listed contracts. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

