using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpaceTraders.API;
using SpaceTraders.Generated.Model;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.Core
{
    public class UniverseSyncManager : MonoBehaviour
    {
        private bool _isSyncing = false;
        public bool IsSyncing => _isSyncing;
        public float Progress { get; private set; }
        
        public int TotalSystemsExpected { get; private set; }
        public int CurrentPage { get; private set; }
        public int TotalPages { get; private set; }

        private CancellationTokenSource _cts;

        private APIService _apiService;
        private DatabaseManager _dbManager;

        [Inject]
        public void Construct(APIService apiService, DatabaseManager dbManager)
        {
            _apiService = apiService;
            _dbManager = dbManager;
        }

        private void Start()
        {
            if (_dbManager == null) return;

            int existingCount = _dbManager.GetIndexedSystemCount();
            Log.Info("[UniverseSyncManager] Initial check. Indexed systems: {Count}", existingCount);
            
            if (existingCount == 0)
            {
                Log.Info("[UniverseSyncManager] DB empty, auto-starting sync...");
                StartSync();
            }
        }

        public void StartSync()
        {
            if (_isSyncing) return;
            _cts = new CancellationTokenSource();
            _ = SyncUniverseAsync(_cts.Token);
        }

        public void StopSync()
        {
            if (!_isSyncing) return;
            _cts?.Cancel();
            _isSyncing = false;
            Log.Info("[UniverseSyncManager] Sync cancel requested.");
        }

        private async Task SyncUniverseAsync(CancellationToken token)
        {
            _isSyncing = true;
            Progress = 0f;
            Log.Info("[UniverseSyncManager] Starting async background universe sync...");

            CurrentPage = 1;
            TotalPages = 1;
            const int limit = 20;

            try
            {
                do
                {
                    if (token.IsCancellationRequested) break;

                    Log.Info("[UniverseSyncManager] Requesting page {Page}...", CurrentPage);
                    var response = await _apiService.GetSystems(CurrentPage, limit);

                    if (response != null && response.Data != null)
                    {
                        var pageResult = UniverseSyncPageWorker.ProcessPage(
                            response,
                            _dbManager.GetAllSystems().ToDictionary(s => s.Symbol, s => s));

                        TotalSystemsExpected = pageResult.TotalSystemsExpected;
                        TotalPages = pageResult.TotalPages;
                        
                        Log.Info("[UniverseSyncManager] Received {Count} systems from API (Total Expected: {Total}).", response.Data.Count, TotalSystemsExpected);

                        _dbManager.StoreSystems(pageResult.IndexedSystems);

                        if (pageResult.JumpGateWaypoints.Count > 0)
                        {
                            _dbManager.StoreJumpGateWaypoints(pageResult.JumpGateWaypoints);
                            Log.Info("[UniverseSyncManager] Registered {Count} JUMP_GATE waypoints for connection sync.", pageResult.JumpGateWaypoints.Count);
                        }

                        int newCount = _dbManager.GetIndexedSystemCount();
                        Log.Info("[UniverseSyncManager] Page {Page} stored. Total indexed: {Total}", CurrentPage, newCount);

                        Progress = (float)CurrentPage / TotalPages;
                        CurrentPage++;
                        
                        // Respect Rate Limits
                        await Task.Delay(1100, token);
                    }
                    else
                    {
                        Log.Error("[UniverseSyncManager] Sync failed on page {Page}: No data in response", CurrentPage);
                        await Task.Delay(5000, token);
                    }

                } while (CurrentPage <= TotalPages);

                // --- Phase 2: Fetch jump gate connection data ---
                await SyncJumpGateConnectionsAsync(token);
            }
            catch (OperationCanceledException)
            {
                Log.Info("[UniverseSyncManager] Sync task cancelled.");
            }
            catch (Exception e)
            {
                Log.Error("[UniverseSyncManager] Sync encountered a critical error: {Error}", e.Message);
            }
            finally
            {
                _isSyncing = false;
                Log.Info("[UniverseSyncManager] Universe sync process terminated.");
            }
        }
        private async Task SyncJumpGateConnectionsAsync(CancellationToken token)
        {
            var pending = _dbManager.GetPendingJumpGates();
            if (pending.Count == 0)
            {
                Log.Info("[UniverseSyncManager] No pending jump gate connections to fetch.");
                return;
            }

            Log.Info("[UniverseSyncManager] Phase 2: Fetching connections for {Count} jump gate(s).", pending.Count);
            int fetched = 0;

            foreach (var gate in pending)
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var response = await _apiService.GetJumpGate(gate.SystemSymbol, gate.WaypointSymbol);
                    var connections = response?.Data?.Connections ?? new List<string>();
                    _dbManager.StoreJumpGateConnections(gate.WaypointSymbol, connections);
                    fetched++;
                    Log.Info("[UniverseSyncManager] Jump gate {WP}: {Count} connection(s) stored. ({Done}/{Total})",
                        gate.WaypointSymbol, connections.Count, fetched, pending.Count);
                }
                catch (Exception e)
                {
                    Log.Warning("[UniverseSyncManager] Failed to fetch jump gate {WP}: {Error}",
                        gate.WaypointSymbol, e.Message);
                    // Store empty list so it is not retried on next launch unless ClearCache is called.
                    _dbManager.StoreJumpGateConnections(gate.WaypointSymbol, new List<string>());
                }

                await Task.Delay(1100, token);
            }

            Log.Info("[UniverseSyncManager] Phase 2 complete. Fetched connections for {Done} jump gate(s).", fetched);
        }
    }

    internal sealed class UniverseSyncPageResult
    {
        public int TotalSystemsExpected { get; set; }
        public int TotalPages { get; set; }
        public List<DatabaseManager.IndexedSystem> IndexedSystems { get; set; } = new List<DatabaseManager.IndexedSystem>();
        public List<(string waypointSymbol, string systemSymbol)> JumpGateWaypoints { get; set; } = new List<(string, string)>();
    }

    internal static class UniverseSyncPageWorker
    {
        public static UniverseSyncPageResult ProcessPage(GetSystems200Response response, Dictionary<string, DatabaseManager.IndexedSystem> existingSystems)
        {
            if (response == null) return new UniverseSyncPageResult();

            var result = new UniverseSyncPageResult
            {
                TotalSystemsExpected = response.Meta?.Total ?? 0,
                TotalPages = ComputeTotalPages(response.Meta)
            };

            foreach (var system in response.Data ?? Enumerable.Empty<SpaceTraders.Generated.Model.System>())
            {
                var indexed = new DatabaseManager.IndexedSystem
                {
                    Symbol = system.Symbol,
                    SectorSymbol = system.SectorSymbol,
                    Type = system.Type.ToString(),
                    X = system.X,
                    Y = system.Y,
                    WaypointCount = system.Waypoints?.Count ?? 0
                };

                if (existingSystems != null && existingSystems.TryGetValue(system.Symbol, out var existing))
                {
                    indexed.KnownFacilities = existing.KnownFacilities;
                }

                result.IndexedSystems.Add(indexed);
                result.JumpGateWaypoints.AddRange(ExtractJumpGateWaypoints(system));
            }

            return result;
        }

        private static int ComputeTotalPages(Meta meta)
        {
            if (meta == null || meta.Limit <= 0) return 1;
            return (int)Math.Ceiling((double)meta.Total / meta.Limit);
        }

        private static IEnumerable<(string waypointSymbol, string systemSymbol)> ExtractJumpGateWaypoints(SpaceTraders.Generated.Model.System system)
        {
            if (system?.Waypoints == null) yield break;

            foreach (var waypoint in system.Waypoints)
            {
                if (waypoint.Type != WaypointType.JUMPGATE) continue;
                yield return (waypoint.Symbol, system.Symbol);
            }
        }
    }
}
