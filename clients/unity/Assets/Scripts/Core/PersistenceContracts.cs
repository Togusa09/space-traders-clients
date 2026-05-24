using System.Collections.Generic;

namespace SpaceTraders.Core
{
    internal interface IApiCacheRepository
    {
        void SetCache(string key, string json);
        string GetCache(string key, long maxAgeSeconds);
        void ClearCache();
    }

    internal interface ISystemIndexRepository
    {
        List<IndexedSystem> GetAllSystems();
        void StoreSystems(IEnumerable<IndexedSystem> systems);
        List<IndexedSystem> SearchSystems(string symbolPattern);
        int GetIndexedSystemCount();
    }

    internal interface IJumpGateRepository
    {
        void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints);
        void StoreJumpGateConnections(string waypointSymbol, List<string> connections);
        List<IndexedJumpGate> GetPendingJumpGates();
        List<IndexedJumpGate> GetAllJumpGateConnections();
        int GetIndexedJumpGateCount();
    }
}
