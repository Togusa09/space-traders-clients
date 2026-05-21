# SpaceTraders.Generated.Api.FleetApi

All URIs are relative to *https://api.spacetraders.io/v2*

| Method | HTTP request | Description |
|--------|--------------|-------------|
| [**CreateChart**](FleetApi.md#createchart) | **POST** /my/ships/{shipSymbol}/chart | Create Chart |
| [**CreateShipShipScan**](FleetApi.md#createshipshipscan) | **POST** /my/ships/{shipSymbol}/scan/ships | Scan Ships |
| [**CreateShipSystemScan**](FleetApi.md#createshipsystemscan) | **POST** /my/ships/{shipSymbol}/scan/systems | Scan Systems |
| [**CreateShipWaypointScan**](FleetApi.md#createshipwaypointscan) | **POST** /my/ships/{shipSymbol}/scan/waypoints | Scan Waypoints |
| [**CreateSurvey**](FleetApi.md#createsurvey) | **POST** /my/ships/{shipSymbol}/survey | Create Survey |
| [**DockShip**](FleetApi.md#dockship) | **POST** /my/ships/{shipSymbol}/dock | Dock Ship |
| [**ExtractResources**](FleetApi.md#extractresources) | **POST** /my/ships/{shipSymbol}/extract | Extract Resources |
| [**ExtractResourcesWithSurvey**](FleetApi.md#extractresourceswithsurvey) | **POST** /my/ships/{shipSymbol}/extract/survey | Extract Resources with Survey |
| [**GetMounts**](FleetApi.md#getmounts) | **GET** /my/ships/{shipSymbol}/mounts | Get Mounts |
| [**GetMyShip**](FleetApi.md#getmyship) | **GET** /my/ships/{shipSymbol} | Get Ship |
| [**GetMyShipCargo**](FleetApi.md#getmyshipcargo) | **GET** /my/ships/{shipSymbol}/cargo | Get Ship Cargo |
| [**GetMyShips**](FleetApi.md#getmyships) | **GET** /my/ships | List Ships |
| [**GetRepairShip**](FleetApi.md#getrepairship) | **GET** /my/ships/{shipSymbol}/repair | Get Repair Ship |
| [**GetScrapShip**](FleetApi.md#getscrapship) | **GET** /my/ships/{shipSymbol}/scrap | Get Scrap Ship |
| [**GetShipCooldown**](FleetApi.md#getshipcooldown) | **GET** /my/ships/{shipSymbol}/cooldown | Get Ship Cooldown |
| [**GetShipModules**](FleetApi.md#getshipmodules) | **GET** /my/ships/{shipSymbol}/modules | Get Ship Modules |
| [**GetShipNav**](FleetApi.md#getshipnav) | **GET** /my/ships/{shipSymbol}/nav | Get Ship Nav |
| [**InstallMount**](FleetApi.md#installmount) | **POST** /my/ships/{shipSymbol}/mounts/install | Install Mount |
| [**InstallShipModule**](FleetApi.md#installshipmodule) | **POST** /my/ships/{shipSymbol}/modules/install | Install Ship Module |
| [**Jettison**](FleetApi.md#jettison) | **POST** /my/ships/{shipSymbol}/jettison | Jettison Cargo |
| [**JumpShip**](FleetApi.md#jumpship) | **POST** /my/ships/{shipSymbol}/jump | Jump Ship |
| [**NavigateShip**](FleetApi.md#navigateship) | **POST** /my/ships/{shipSymbol}/navigate | Navigate Ship |
| [**NegotiateContract**](FleetApi.md#negotiatecontract) | **POST** /my/ships/{shipSymbol}/negotiate/contract | Negotiate Contract |
| [**OrbitShip**](FleetApi.md#orbitship) | **POST** /my/ships/{shipSymbol}/orbit | Orbit Ship |
| [**PatchShipNav**](FleetApi.md#patchshipnav) | **PATCH** /my/ships/{shipSymbol}/nav | Patch Ship Nav |
| [**PurchaseCargo**](FleetApi.md#purchasecargo) | **POST** /my/ships/{shipSymbol}/purchase | Purchase Cargo |
| [**PurchaseShip**](FleetApi.md#purchaseship) | **POST** /my/ships | Purchase Ship |
| [**RefuelShip**](FleetApi.md#refuelship) | **POST** /my/ships/{shipSymbol}/refuel | Refuel Ship |
| [**RemoveMount**](FleetApi.md#removemount) | **POST** /my/ships/{shipSymbol}/mounts/remove | Remove Mount |
| [**RemoveShipModule**](FleetApi.md#removeshipmodule) | **POST** /my/ships/{shipSymbol}/modules/remove | Remove Ship Module |
| [**RepairShip**](FleetApi.md#repairship) | **POST** /my/ships/{shipSymbol}/repair | Repair Ship |
| [**ScrapShip**](FleetApi.md#scrapship) | **POST** /my/ships/{shipSymbol}/scrap | Scrap Ship |
| [**SellCargo**](FleetApi.md#sellcargo) | **POST** /my/ships/{shipSymbol}/sell | Sell Cargo |
| [**ShipRefine**](FleetApi.md#shiprefine) | **POST** /my/ships/{shipSymbol}/refine | Ship Refine |
| [**SiphonResources**](FleetApi.md#siphonresources) | **POST** /my/ships/{shipSymbol}/siphon | Siphon Resources |
| [**TransferCargo**](FleetApi.md#transfercargo) | **POST** /my/ships/{shipSymbol}/transfer | Transfer Cargo |
| [**WarpShip**](FleetApi.md#warpship) | **POST** /my/ships/{shipSymbol}/warp | Warp Ship |

<a id="createchart"></a>
# **CreateChart**
> CreateChart201Response CreateChart (string shipSymbol)

Create Chart

Command a ship to chart the waypoint at its current location.  Most waypoints in the universe are uncharted by default. These waypoints have their traits hidden until they have been charted by a ship.  Charting a waypoint will record your agent as the one who created the chart, and all other agents would also be able to see the waypoint's traits. Charting a waypoint gives you a one time reward of credits based on the rarity of the waypoint's traits.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class CreateChartExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Create Chart
                CreateChart201Response result = apiInstance.CreateChart(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.CreateChart: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateChartWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create Chart
    ApiResponse<CreateChart201Response> response = apiInstance.CreateChartWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.CreateChartWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**CreateChart201Response**](CreateChart201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Created |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createshipshipscan"></a>
# **CreateShipShipScan**
> CreateShipShipScan201Response CreateShipShipScan (string shipSymbol)

Scan Ships

Scan for nearby ships, retrieving information for all ships in range.  Requires a ship to have the `Sensor Array` mount installed to use.  The ship will enter a cooldown after using this function, during which it cannot execute certain actions.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class CreateShipShipScanExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Scan Ships
                CreateShipShipScan201Response result = apiInstance.CreateShipShipScan(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.CreateShipShipScan: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateShipShipScanWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Scan Ships
    ApiResponse<CreateShipShipScan201Response> response = apiInstance.CreateShipShipScanWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.CreateShipShipScanWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**CreateShipShipScan201Response**](CreateShipShipScan201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully scanned for nearby ships. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createshipsystemscan"></a>
# **CreateShipSystemScan**
> CreateShipSystemScan201Response CreateShipSystemScan (string shipSymbol)

Scan Systems

Scan for nearby systems, retrieving information on the systems' distance from the ship and their waypoints. Requires a ship to have the `Sensor Array` mount installed to use.  The ship will enter a cooldown after using this function, during which it cannot execute certain actions.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class CreateShipSystemScanExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Scan Systems
                CreateShipSystemScan201Response result = apiInstance.CreateShipSystemScan(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.CreateShipSystemScan: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateShipSystemScanWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Scan Systems
    ApiResponse<CreateShipSystemScan201Response> response = apiInstance.CreateShipSystemScanWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.CreateShipSystemScanWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**CreateShipSystemScan201Response**](CreateShipSystemScan201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully scanned for nearby systems. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createshipwaypointscan"></a>
# **CreateShipWaypointScan**
> CreateShipWaypointScan201Response CreateShipWaypointScan (string shipSymbol)

Scan Waypoints

Scan for nearby waypoints, retrieving detailed information on each waypoint in range. Scanning uncharted waypoints will allow you to ignore their uncharted state and will list the waypoints' traits.  Requires a ship to have the `Sensor Array` mount installed to use.  The ship will enter a cooldown after using this function, during which it cannot execute certain actions.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class CreateShipWaypointScanExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Scan Waypoints
                CreateShipWaypointScan201Response result = apiInstance.CreateShipWaypointScan(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.CreateShipWaypointScan: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateShipWaypointScanWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Scan Waypoints
    ApiResponse<CreateShipWaypointScan201Response> response = apiInstance.CreateShipWaypointScanWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.CreateShipWaypointScanWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**CreateShipWaypointScan201Response**](CreateShipWaypointScan201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully scanned for nearby waypoints. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="createsurvey"></a>
# **CreateSurvey**
> CreateSurvey201Response CreateSurvey (string shipSymbol)

Create Survey

Create surveys on a waypoint that can be extracted such as asteroid fields. A survey focuses on specific types of deposits from the extracted location. When ships extract using this survey, they are guaranteed to procure a high amount of one of the goods in the survey.  In order to use a survey, send the entire survey details in the body of the extract request.  Each survey may have multiple deposits, and if a symbol shows up more than once, that indicates a higher chance of extracting that resource.  Your ship will enter a cooldown after surveying in which it is unable to perform certain actions. Surveys will eventually expire after a period of time or will be exhausted after being extracted several times based on the survey's size. Multiple ships can use the same survey for extraction.  A ship must have the `Surveyor` mount installed in order to use this function.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class CreateSurveyExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Create Survey
                CreateSurvey201Response result = apiInstance.CreateSurvey(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.CreateSurvey: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the CreateSurveyWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Create Survey
    ApiResponse<CreateSurvey201Response> response = apiInstance.CreateSurveyWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.CreateSurveyWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**CreateSurvey201Response**](CreateSurvey201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Surveys has been created. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="dockship"></a>
# **DockShip**
> DockShip200Response DockShip (string shipSymbol)

Dock Ship

Attempt to dock your ship at its current location. Docking will only succeed if your ship is capable of docking at the time of the request.  Docked ships can access elements in their current location, such as the market or a shipyard, but cannot do actions that require the ship to be above surface such as navigating or extracting.  The endpoint is idempotent - successive calls will succeed even if the ship is already docked.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class DockShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Dock Ship
                DockShip200Response result = apiInstance.DockShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.DockShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the DockShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Dock Ship
    ApiResponse<DockShip200Response> response = apiInstance.DockShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.DockShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**DockShip200Response**](DockShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The ship has successfully docked at its current location. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="extractresources"></a>
# **ExtractResources**
> ExtractResources201Response ExtractResources (string shipSymbol, ExtractResourcesRequest extractResourcesRequest = null)

Extract Resources

Extract resources from a waypoint that can be extracted, such as asteroid fields, into your ship. Send an optional survey as the payload to target specific yields.  The ship must be in orbit to be able to extract and must have mining equipments installed that can extract goods, such as the `Gas Siphon` mount for gas-based goods or `Mining Laser` mount for ore-based goods.  The survey property is now deprecated. See the `extract/survey` endpoint for more details.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class ExtractResourcesExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var extractResourcesRequest = new ExtractResourcesRequest(); // ExtractResourcesRequest |  (optional) 

            try
            {
                // Extract Resources
                ExtractResources201Response result = apiInstance.ExtractResources(shipSymbol, extractResourcesRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.ExtractResources: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ExtractResourcesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Extract Resources
    ApiResponse<ExtractResources201Response> response = apiInstance.ExtractResourcesWithHttpInfo(shipSymbol, extractResourcesRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.ExtractResourcesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **extractResourcesRequest** | [**ExtractResourcesRequest**](ExtractResourcesRequest.md) |  | [optional]  |

### Return type

[**ExtractResources201Response**](ExtractResources201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Extracted successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="extractresourceswithsurvey"></a>
# **ExtractResourcesWithSurvey**
> ExtractResourcesWithSurvey201Response ExtractResourcesWithSurvey (string shipSymbol, Survey survey = null)

Extract Resources with Survey

Use a survey when extracting resources from a waypoint. This endpoint requires a survey as the payload, which allows your ship to extract specific yields.  Send the full survey object as the payload which will be validated according to the signature. If the signature is invalid, or any properties of the survey are changed, the request will fail.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class ExtractResourcesWithSurveyExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var survey = new Survey(); // Survey |  (optional) 

            try
            {
                // Extract Resources with Survey
                ExtractResourcesWithSurvey201Response result = apiInstance.ExtractResourcesWithSurvey(shipSymbol, survey);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.ExtractResourcesWithSurvey: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ExtractResourcesWithSurveyWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Extract Resources with Survey
    ApiResponse<ExtractResourcesWithSurvey201Response> response = apiInstance.ExtractResourcesWithSurveyWithHttpInfo(shipSymbol, survey);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.ExtractResourcesWithSurveyWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **survey** | [**Survey**](Survey.md) |  | [optional]  |

### Return type

[**ExtractResourcesWithSurvey201Response**](ExtractResourcesWithSurvey201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Extracted successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getmounts"></a>
# **GetMounts**
> GetMounts200Response GetMounts (string shipSymbol)

Get Mounts

Get the mounts installed on a ship.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetMountsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship's symbol.

            try
            {
                // Get Mounts
                GetMounts200Response result = apiInstance.GetMounts(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetMounts: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMountsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Mounts
    ApiResponse<GetMounts200Response> response = apiInstance.GetMountsWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetMountsWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship&#39;s symbol. |  |

### Return type

[**GetMounts200Response**](GetMounts200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Got installed mounts. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getmyship"></a>
# **GetMyShip**
> GetMyShip200Response GetMyShip (string shipSymbol)

Get Ship

Retrieve the details of a ship under your agent's ownership.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetMyShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Get Ship
                GetMyShip200Response result = apiInstance.GetMyShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetMyShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMyShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Ship
    ApiResponse<GetMyShip200Response> response = apiInstance.GetMyShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetMyShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**GetMyShip200Response**](GetMyShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fetched ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getmyshipcargo"></a>
# **GetMyShipCargo**
> GetMyShipCargo200Response GetMyShipCargo (string shipSymbol)

Get Ship Cargo

Retrieve the cargo of a ship under your agent's ownership.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetMyShipCargoExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Get Ship Cargo
                GetMyShipCargo200Response result = apiInstance.GetMyShipCargo(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetMyShipCargo: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMyShipCargoWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Ship Cargo
    ApiResponse<GetMyShipCargo200Response> response = apiInstance.GetMyShipCargoWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetMyShipCargoWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**GetMyShipCargo200Response**](GetMyShipCargo200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fetched ship&#39;s cargo. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getmyships"></a>
# **GetMyShips**
> GetMyShips200Response GetMyShips (int? page = null, int? limit = null)

List Ships

Return a paginated list of all of ships under your agent's ownership.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetMyShipsExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var page = 1;  // int? | What entry offset to request (optional)  (default to 1)
            var limit = 10;  // int? | How many entries to return per page (optional)  (default to 10)

            try
            {
                // List Ships
                GetMyShips200Response result = apiInstance.GetMyShips(page, limit);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetMyShips: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetMyShipsWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // List Ships
    ApiResponse<GetMyShips200Response> response = apiInstance.GetMyShipsWithHttpInfo(page, limit);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetMyShipsWithHttpInfo: " + e.Message);
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

[**GetMyShips200Response**](GetMyShips200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully listed ships. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getrepairship"></a>
# **GetRepairShip**
> GetRepairShip200Response GetRepairShip (string shipSymbol)

Get Repair Ship

Get the cost of repairing a ship.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetRepairShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Get Repair Ship
                GetRepairShip200Response result = apiInstance.GetRepairShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetRepairShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetRepairShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Repair Ship
    ApiResponse<GetRepairShip200Response> response = apiInstance.GetRepairShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetRepairShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**GetRepairShip200Response**](GetRepairShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully retrieved the cost of repairing a ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getscrapship"></a>
# **GetScrapShip**
> GetScrapShip200Response GetScrapShip (string shipSymbol)

Get Scrap Ship

Get the amount of value that will be returned when scrapping a ship.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetScrapShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Get Scrap Ship
                GetScrapShip200Response result = apiInstance.GetScrapShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetScrapShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetScrapShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Scrap Ship
    ApiResponse<GetScrapShip200Response> response = apiInstance.GetScrapShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetScrapShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**GetScrapShip200Response**](GetScrapShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully retrieved the amount of value that will be returned when scrapping a ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getshipcooldown"></a>
# **GetShipCooldown**
> GetShipCooldown200Response GetShipCooldown (string shipSymbol)

Get Ship Cooldown

Retrieve the details of your ship's reactor cooldown. Some actions such as activating your jump drive, scanning, or extracting resources taxes your reactor and results in a cooldown.  Your ship cannot perform additional actions until your cooldown has expired. The duration of your cooldown is relative to the power consumption of the related modules or mounts for the action taken.  Response returns a 204 status code (no-content) when the ship has no cooldown.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetShipCooldownExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Get Ship Cooldown
                GetShipCooldown200Response result = apiInstance.GetShipCooldown(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetShipCooldown: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetShipCooldownWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Ship Cooldown
    ApiResponse<GetShipCooldown200Response> response = apiInstance.GetShipCooldownWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetShipCooldownWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**GetShipCooldown200Response**](GetShipCooldown200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully fetched ship&#39;s cooldown. |  -  |
| **204** | No cooldown. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getshipmodules"></a>
# **GetShipModules**
> GetShipModules200Response GetShipModules (string shipSymbol)

Get Ship Modules

Get the modules installed on a ship.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetShipModulesExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AccountToken
            config.AccessToken = "YOUR_BEARER_TOKEN";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship

            try
            {
                // Get Ship Modules
                GetShipModules200Response result = apiInstance.GetShipModules(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetShipModules: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetShipModulesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Ship Modules
    ApiResponse<GetShipModules200Response> response = apiInstance.GetShipModulesWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetShipModulesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship |  |

### Return type

[**GetShipModules200Response**](GetShipModules200Response.md)

### Authorization

[AccountToken](../README.md#AccountToken), [AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Successfully retrieved ship modules. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="getshipnav"></a>
# **GetShipNav**
> GetShipNav200Response GetShipNav (string shipSymbol)

Get Ship Nav

Get the current nav status of a ship.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class GetShipNavExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Get Ship Nav
                GetShipNav200Response result = apiInstance.GetShipNav(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.GetShipNav: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the GetShipNavWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Get Ship Nav
    ApiResponse<GetShipNav200Response> response = apiInstance.GetShipNavWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.GetShipNavWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**GetShipNav200Response**](GetShipNav200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The current nav status of the ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="installmount"></a>
# **InstallMount**
> InstallMount201Response InstallMount (string shipSymbol, InstallMountRequest installMountRequest = null)

Install Mount

Install a mount on a ship.  In order to install a mount, the ship must be docked and located in a waypoint that has a `Shipyard` trait. The ship also must have the mount to install in its cargo hold.  An installation fee will be deduced by the Shipyard for installing the mount on the ship. 

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class InstallMountExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship's symbol.
            var installMountRequest = new InstallMountRequest(); // InstallMountRequest |  (optional) 

            try
            {
                // Install Mount
                InstallMount201Response result = apiInstance.InstallMount(shipSymbol, installMountRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.InstallMount: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the InstallMountWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Install Mount
    ApiResponse<InstallMount201Response> response = apiInstance.InstallMountWithHttpInfo(shipSymbol, installMountRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.InstallMountWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship&#39;s symbol. |  |
| **installMountRequest** | [**InstallMountRequest**](InstallMountRequest.md) |  | [optional]  |

### Return type

[**InstallMount201Response**](InstallMount201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully installed the mount. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="installshipmodule"></a>
# **InstallShipModule**
> InstallShipModule201Response InstallShipModule (string shipSymbol, InstallShipModuleRequest installShipModuleRequest = null)

Install Ship Module

Install a module on a ship. The module must be in your cargo.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class InstallShipModuleExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AccountToken
            config.AccessToken = "YOUR_BEARER_TOKEN";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship
            var installShipModuleRequest = new InstallShipModuleRequest(); // InstallShipModuleRequest |  (optional) 

            try
            {
                // Install Ship Module
                InstallShipModule201Response result = apiInstance.InstallShipModule(shipSymbol, installShipModuleRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.InstallShipModule: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the InstallShipModuleWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Install Ship Module
    ApiResponse<InstallShipModule201Response> response = apiInstance.InstallShipModuleWithHttpInfo(shipSymbol, installShipModuleRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.InstallShipModuleWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship |  |
| **installShipModuleRequest** | [**InstallShipModuleRequest**](InstallShipModuleRequest.md) |  | [optional]  |

### Return type

[**InstallShipModule201Response**](InstallShipModule201Response.md)

### Authorization

[AccountToken](../README.md#AccountToken), [AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully installed the module on the ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="jettison"></a>
# **Jettison**
> Jettison200Response Jettison (string shipSymbol, JettisonRequest jettisonRequest = null)

Jettison Cargo

Jettison cargo from your ship's cargo hold.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class JettisonExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var jettisonRequest = new JettisonRequest(); // JettisonRequest |  (optional) 

            try
            {
                // Jettison Cargo
                Jettison200Response result = apiInstance.Jettison(shipSymbol, jettisonRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.Jettison: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the JettisonWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Jettison Cargo
    ApiResponse<Jettison200Response> response = apiInstance.JettisonWithHttpInfo(shipSymbol, jettisonRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.JettisonWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **jettisonRequest** | [**JettisonRequest**](JettisonRequest.md) |  | [optional]  |

### Return type

[**Jettison200Response**](Jettison200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Jettison successful. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="jumpship"></a>
# **JumpShip**
> JumpShip200Response JumpShip (string shipSymbol, JumpShipRequest jumpShipRequest = null)

Jump Ship

Jump your ship instantly to a target connected waypoint. The ship must be in orbit to execute a jump.  A unit of antimatter is purchased and consumed from the market when jumping. The price of antimatter is determined by the market and is subject to change. A ship can only jump to connected waypoints

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class JumpShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var jumpShipRequest = new JumpShipRequest(); // JumpShipRequest |  (optional) 

            try
            {
                // Jump Ship
                JumpShip200Response result = apiInstance.JumpShip(shipSymbol, jumpShipRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.JumpShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the JumpShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Jump Ship
    ApiResponse<JumpShip200Response> response = apiInstance.JumpShipWithHttpInfo(shipSymbol, jumpShipRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.JumpShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **jumpShipRequest** | [**JumpShipRequest**](JumpShipRequest.md) |  | [optional]  |

### Return type

[**JumpShip200Response**](JumpShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Jump successful. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="navigateship"></a>
# **NavigateShip**
> NavigateShip200Response NavigateShip (string shipSymbol, NavigateShipRequest navigateShipRequest = null)

Navigate Ship

Navigate to a target destination. The ship must be in orbit to use this function. The destination waypoint must be within the same system as the ship's current location. Navigating will consume the necessary fuel from the ship's manifest based on the distance to the target waypoint.  The returned response will detail the route information including the expected time of arrival. Most ship actions are unavailable until the ship has arrived at it's destination.  To travel between systems, see the ship's Warp or Jump actions.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class NavigateShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var navigateShipRequest = new NavigateShipRequest(); // NavigateShipRequest |  (optional) 

            try
            {
                // Navigate Ship
                NavigateShip200Response result = apiInstance.NavigateShip(shipSymbol, navigateShipRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.NavigateShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the NavigateShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Navigate Ship
    ApiResponse<NavigateShip200Response> response = apiInstance.NavigateShipWithHttpInfo(shipSymbol, navigateShipRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.NavigateShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **navigateShipRequest** | [**NavigateShipRequest**](NavigateShipRequest.md) |  | [optional]  |

### Return type

[**NavigateShip200Response**](NavigateShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The successful transit information including the route details and changes to ship fuel. The route includes the expected time of arrival. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="negotiatecontract"></a>
# **NegotiateContract**
> NegotiateContract200Response NegotiateContract (string shipSymbol)

Negotiate Contract

Negotiate a new contract with the HQ.  In order to negotiate a new contract, an agent must not have ongoing or offered contracts over the allowed maximum amount. Currently the maximum contracts an agent can have at a time is 1.  Once a contract is negotiated, it is added to the list of contracts offered to the agent, which the agent can then accept.   The ship must be present at any waypoint with a faction present to negotiate a contract with that faction.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class NegotiateContractExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship's symbol.

            try
            {
                // Negotiate Contract
                NegotiateContract200Response result = apiInstance.NegotiateContract(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.NegotiateContract: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the NegotiateContractWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Negotiate Contract
    ApiResponse<NegotiateContract200Response> response = apiInstance.NegotiateContractWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.NegotiateContractWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship&#39;s symbol. |  |

### Return type

[**NegotiateContract200Response**](NegotiateContract200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully negotiated a new contract. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="orbitship"></a>
# **OrbitShip**
> OrbitShip200Response OrbitShip (string shipSymbol)

Orbit Ship

Attempt to move your ship into orbit at its current location. The request will only succeed if your ship is capable of moving into orbit at the time of the request.  Orbiting ships are able to do actions that require the ship to be above surface such as navigating or extracting, but cannot access elements in their current waypoint, such as the market or a shipyard.  The endpoint is idempotent - successive calls will succeed even if the ship is already in orbit.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class OrbitShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.

            try
            {
                // Orbit Ship
                OrbitShip200Response result = apiInstance.OrbitShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.OrbitShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the OrbitShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Orbit Ship
    ApiResponse<OrbitShip200Response> response = apiInstance.OrbitShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.OrbitShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |

### Return type

[**OrbitShip200Response**](OrbitShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The ship has successfully moved into orbit at its current location. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="patchshipnav"></a>
# **PatchShipNav**
> PatchShipNav200Response PatchShipNav (string shipSymbol, PatchShipNavRequest patchShipNavRequest = null)

Patch Ship Nav

Update the nav configuration of a ship.  Currently only supports configuring the Flight Mode of the ship, which affects its speed and fuel consumption.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class PatchShipNavExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var patchShipNavRequest = new PatchShipNavRequest(); // PatchShipNavRequest |  (optional) 

            try
            {
                // Patch Ship Nav
                PatchShipNav200Response result = apiInstance.PatchShipNav(shipSymbol, patchShipNavRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.PatchShipNav: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the PatchShipNavWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Patch Ship Nav
    ApiResponse<PatchShipNav200Response> response = apiInstance.PatchShipNavWithHttpInfo(shipSymbol, patchShipNavRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.PatchShipNavWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **patchShipNavRequest** | [**PatchShipNavRequest**](PatchShipNavRequest.md) |  | [optional]  |

### Return type

[**PatchShipNav200Response**](PatchShipNav200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The updated data of the ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="purchasecargo"></a>
# **PurchaseCargo**
> PurchaseCargo201Response PurchaseCargo (string shipSymbol, PurchaseCargoRequest purchaseCargoRequest = null)

Purchase Cargo

Purchase cargo from a market.  The ship must be docked in a waypoint that has `Marketplace` trait, and the market must be selling a good to be able to purchase it.  The maximum amount of units of a good that can be purchased in each transaction are denoted by the `tradeVolume` value of the good, which can be viewed by using the Get Market action.  Purchased goods are added to the ship's cargo hold.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class PurchaseCargoExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship's symbol.
            var purchaseCargoRequest = new PurchaseCargoRequest(); // PurchaseCargoRequest |  (optional) 

            try
            {
                // Purchase Cargo
                PurchaseCargo201Response result = apiInstance.PurchaseCargo(shipSymbol, purchaseCargoRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.PurchaseCargo: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the PurchaseCargoWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Purchase Cargo
    ApiResponse<PurchaseCargo201Response> response = apiInstance.PurchaseCargoWithHttpInfo(shipSymbol, purchaseCargoRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.PurchaseCargoWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship&#39;s symbol. |  |
| **purchaseCargoRequest** | [**PurchaseCargoRequest**](PurchaseCargoRequest.md) |  | [optional]  |

### Return type

[**PurchaseCargo201Response**](PurchaseCargo201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Purchased goods successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="purchaseship"></a>
# **PurchaseShip**
> PurchaseShip201Response PurchaseShip (PurchaseShipRequest purchaseShipRequest = null)

Purchase Ship

Purchase a ship from a Shipyard. In order to use this function, a ship under your agent's ownership must be in a waypoint that has the `Shipyard` trait, and the Shipyard must sell the type of the desired ship.  Shipyards typically offer ship types, which are predefined templates of ships that have dedicated roles. A template comes with a preset of an engine, a reactor, and a frame. It may also include a few modules and mounts.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class PurchaseShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var purchaseShipRequest = new PurchaseShipRequest(); // PurchaseShipRequest |  (optional) 

            try
            {
                // Purchase Ship
                PurchaseShip201Response result = apiInstance.PurchaseShip(purchaseShipRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.PurchaseShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the PurchaseShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Purchase Ship
    ApiResponse<PurchaseShip201Response> response = apiInstance.PurchaseShipWithHttpInfo(purchaseShipRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.PurchaseShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **purchaseShipRequest** | [**PurchaseShipRequest**](PurchaseShipRequest.md) |  | [optional]  |

### Return type

[**PurchaseShip201Response**](PurchaseShip201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Purchased ship successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="refuelship"></a>
# **RefuelShip**
> RefuelShip200Response RefuelShip (string shipSymbol, RefuelShipRequest refuelShipRequest = null)

Refuel Ship

Refuel your ship by buying fuel from the local market.  Requires the ship to be docked in a waypoint that has the `Marketplace` trait, and the market must be selling fuel in order to refuel.  Each fuel bought from the market replenishes 100 units in your ship's fuel.  Ships will always be refuel to their frame's maximum fuel capacity when using this action.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class RefuelShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var refuelShipRequest = new RefuelShipRequest(); // RefuelShipRequest |  (optional) 

            try
            {
                // Refuel Ship
                RefuelShip200Response result = apiInstance.RefuelShip(shipSymbol, refuelShipRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.RefuelShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RefuelShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Refuel Ship
    ApiResponse<RefuelShip200Response> response = apiInstance.RefuelShipWithHttpInfo(shipSymbol, refuelShipRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.RefuelShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **refuelShipRequest** | [**RefuelShipRequest**](RefuelShipRequest.md) |  | [optional]  |

### Return type

[**RefuelShip200Response**](RefuelShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Refueled successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="removemount"></a>
# **RemoveMount**
> RemoveMount201Response RemoveMount (string shipSymbol, RemoveMountRequest removeMountRequest = null)

Remove Mount

Remove a mount from a ship.  The ship must be docked in a waypoint that has the `Shipyard` trait, and must have the desired mount that it wish to remove installed.  A removal fee will be deduced from the agent by the Shipyard.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class RemoveMountExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship's symbol.
            var removeMountRequest = new RemoveMountRequest(); // RemoveMountRequest |  (optional) 

            try
            {
                // Remove Mount
                RemoveMount201Response result = apiInstance.RemoveMount(shipSymbol, removeMountRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.RemoveMount: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RemoveMountWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Remove Mount
    ApiResponse<RemoveMount201Response> response = apiInstance.RemoveMountWithHttpInfo(shipSymbol, removeMountRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.RemoveMountWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship&#39;s symbol. |  |
| **removeMountRequest** | [**RemoveMountRequest**](RemoveMountRequest.md) |  | [optional]  |

### Return type

[**RemoveMount201Response**](RemoveMount201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully removed the mount. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="removeshipmodule"></a>
# **RemoveShipModule**
> RemoveModule201Response RemoveShipModule (string shipSymbol, RemoveShipModuleRequest removeShipModuleRequest = null)

Remove Ship Module

Remove a module from a ship. The module will be placed in cargo.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class RemoveShipModuleExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AccountToken
            config.AccessToken = "YOUR_BEARER_TOKEN";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship
            var removeShipModuleRequest = new RemoveShipModuleRequest(); // RemoveShipModuleRequest |  (optional) 

            try
            {
                // Remove Ship Module
                RemoveModule201Response result = apiInstance.RemoveShipModule(shipSymbol, removeShipModuleRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.RemoveShipModule: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RemoveShipModuleWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Remove Ship Module
    ApiResponse<RemoveModule201Response> response = apiInstance.RemoveShipModuleWithHttpInfo(shipSymbol, removeShipModuleRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.RemoveShipModuleWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship |  |
| **removeShipModuleRequest** | [**RemoveShipModuleRequest**](RemoveShipModuleRequest.md) |  | [optional]  |

### Return type

[**RemoveModule201Response**](RemoveModule201Response.md)

### Authorization

[AccountToken](../README.md#AccountToken), [AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Successfully removed the module from the ship. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="repairship"></a>
# **RepairShip**
> RepairShip200Response RepairShip (string shipSymbol)

Repair Ship

Repair a ship, restoring the ship to maximum condition. The ship must be docked at a waypoint that has the `Shipyard` trait in order to use this function. To preview the cost of repairing the ship, use the Get action.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class RepairShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Repair Ship
                RepairShip200Response result = apiInstance.RepairShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.RepairShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the RepairShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Repair Ship
    ApiResponse<RepairShip200Response> response = apiInstance.RepairShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.RepairShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**RepairShip200Response**](RepairShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Ship repaired successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="scrapship"></a>
# **ScrapShip**
> ScrapShip200Response ScrapShip (string shipSymbol)

Scrap Ship

Scrap a ship, removing it from the game and returning a portion of the ship's value to the agent. The ship must be docked in a waypoint that has the `Shipyard` trait in order to use this function. To preview the amount of value that will be returned, use the Get Ship action.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class ScrapShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Scrap Ship
                ScrapShip200Response result = apiInstance.ScrapShip(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.ScrapShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ScrapShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Scrap Ship
    ApiResponse<ScrapShip200Response> response = apiInstance.ScrapShipWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.ScrapShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**ScrapShip200Response**](ScrapShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Ship scrapped successfully. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="sellcargo"></a>
# **SellCargo**
> SellCargo201Response SellCargo (string shipSymbol, SellCargoRequest sellCargoRequest = null)

Sell Cargo

Sell cargo in your ship to a market that trades this cargo. The ship must be docked in a waypoint that has the `Marketplace` trait in order to use this function.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class SellCargoExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | Symbol of a ship.
            var sellCargoRequest = new SellCargoRequest(); // SellCargoRequest |  (optional) 

            try
            {
                // Sell Cargo
                SellCargo201Response result = apiInstance.SellCargo(shipSymbol, sellCargoRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.SellCargo: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SellCargoWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Sell Cargo
    ApiResponse<SellCargo201Response> response = apiInstance.SellCargoWithHttpInfo(shipSymbol, sellCargoRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.SellCargoWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | Symbol of a ship. |  |
| **sellCargoRequest** | [**SellCargoRequest**](SellCargoRequest.md) |  | [optional]  |

### Return type

[**SellCargo201Response**](SellCargo201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Cargo was successfully sold. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="shiprefine"></a>
# **ShipRefine**
> ShipRefine201Response ShipRefine (string shipSymbol, ShipRefineRequest shipRefineRequest = null)

Ship Refine

Attempt to refine the raw materials on your ship. The request will only succeed if your ship is capable of refining at the time of the request. In order to be able to refine, a ship must have goods that can be refined and have installed a `Refinery` module that can refine it.  When refining, 100 basic goods will be converted into 10 processed goods.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class ShipRefineExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The symbol of the ship.
            var shipRefineRequest = new ShipRefineRequest(); // ShipRefineRequest |  (optional) 

            try
            {
                // Ship Refine
                ShipRefine201Response result = apiInstance.ShipRefine(shipSymbol, shipRefineRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.ShipRefine: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the ShipRefineWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Ship Refine
    ApiResponse<ShipRefine201Response> response = apiInstance.ShipRefineWithHttpInfo(shipSymbol, shipRefineRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.ShipRefineWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The symbol of the ship. |  |
| **shipRefineRequest** | [**ShipRefineRequest**](ShipRefineRequest.md) |  | [optional]  |

### Return type

[**ShipRefine201Response**](ShipRefine201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | The ship has successfully refined goods. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="siphonresources"></a>
# **SiphonResources**
> SiphonResources201Response SiphonResources (string shipSymbol)

Siphon Resources

Siphon gases or other resources from gas giants.  The ship must be in orbit to be able to siphon and must have siphon mounts and a gas processor installed.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class SiphonResourcesExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.

            try
            {
                // Siphon Resources
                SiphonResources201Response result = apiInstance.SiphonResources(shipSymbol);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.SiphonResources: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the SiphonResourcesWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Siphon Resources
    ApiResponse<SiphonResources201Response> response = apiInstance.SiphonResourcesWithHttpInfo(shipSymbol);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.SiphonResourcesWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |

### Return type

[**SiphonResources201Response**](SiphonResources201Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **201** | Siphon successful. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="transfercargo"></a>
# **TransferCargo**
> TransferCargo200Response TransferCargo (string shipSymbol, TransferCargoRequest transferCargoRequest = null)

Transfer Cargo

Transfer cargo between ships.  The receiving ship must be in the same waypoint as the transferring ship, and it must able to hold the additional cargo after the transfer is complete. Both ships also must be in the same state, either both are docked or both are orbiting.  The response body's cargo shows the cargo of the transferring ship after the transfer is complete.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class TransferCargoExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The transferring ship's symbol.
            var transferCargoRequest = new TransferCargoRequest(); // TransferCargoRequest |  (optional) 

            try
            {
                // Transfer Cargo
                TransferCargo200Response result = apiInstance.TransferCargo(shipSymbol, transferCargoRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.TransferCargo: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the TransferCargoWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Transfer Cargo
    ApiResponse<TransferCargo200Response> response = apiInstance.TransferCargoWithHttpInfo(shipSymbol, transferCargoRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.TransferCargoWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The transferring ship&#39;s symbol. |  |
| **transferCargoRequest** | [**TransferCargoRequest**](TransferCargoRequest.md) |  | [optional]  |

### Return type

[**TransferCargo200Response**](TransferCargo200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | Transfer successful. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a id="warpship"></a>
# **WarpShip**
> WarpShip200Response WarpShip (string shipSymbol, NavigateShipRequest navigateShipRequest = null)

Warp Ship

Warp your ship to a target destination in another system. The ship must be in orbit to use this function and must have the `Warp Drive` module installed. Warping will consume the necessary fuel from the ship's manifest.  The returned response will detail the route information including the expected time of arrival. Most ship actions are unavailable until the ship has arrived at its destination.

### Example
```csharp
using System.Collections.Generic;
using System.Diagnostics;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using SpaceTraders.Generated.Model;

namespace Example
{
    public class WarpShipExample
    {
        public static void Main()
        {
            Configuration config = new Configuration();
            config.BasePath = "https://api.spacetraders.io/v2";
            // Configure Bearer token for authorization: AgentToken
            config.AccessToken = "YOUR_BEARER_TOKEN";

            var apiInstance = new FleetApi(config);
            var shipSymbol = "shipSymbol_example";  // string | The ship symbol.
            var navigateShipRequest = new NavigateShipRequest(); // NavigateShipRequest |  (optional) 

            try
            {
                // Warp Ship
                WarpShip200Response result = apiInstance.WarpShip(shipSymbol, navigateShipRequest);
                Debug.WriteLine(result);
            }
            catch (ApiException  e)
            {
                Debug.Print("Exception when calling FleetApi.WarpShip: " + e.Message);
                Debug.Print("Status Code: " + e.ErrorCode);
                Debug.Print(e.StackTrace);
            }
        }
    }
}
```

#### Using the WarpShipWithHttpInfo variant
This returns an ApiResponse object which contains the response data, status code and headers.

```csharp
try
{
    // Warp Ship
    ApiResponse<WarpShip200Response> response = apiInstance.WarpShipWithHttpInfo(shipSymbol, navigateShipRequest);
    Debug.Write("Status Code: " + response.StatusCode);
    Debug.Write("Response Headers: " + response.Headers);
    Debug.Write("Response Body: " + response.Data);
}
catch (ApiException e)
{
    Debug.Print("Exception when calling FleetApi.WarpShipWithHttpInfo: " + e.Message);
    Debug.Print("Status Code: " + e.ErrorCode);
    Debug.Print(e.StackTrace);
}
```

### Parameters

| Name | Type | Description | Notes |
|------|------|-------------|-------|
| **shipSymbol** | **string** | The ship symbol. |  |
| **navigateShipRequest** | [**NavigateShipRequest**](NavigateShipRequest.md) |  | [optional]  |

### Return type

[**WarpShip200Response**](WarpShip200Response.md)

### Authorization

[AgentToken](../README.md#AgentToken)

### HTTP request headers

 - **Content-Type**: application/json
 - **Accept**: application/json


### HTTP response details
| Status code | Description | Response headers |
|-------------|-------------|------------------|
| **200** | The successful transit information including the route details and changes to ship fuel. The route includes the expected time of arrival. |  -  |

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

