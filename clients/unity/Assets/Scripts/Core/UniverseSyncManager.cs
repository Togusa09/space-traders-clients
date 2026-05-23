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
                        TotalSystemsExpected = response.Meta.Total;
                        TotalPages = (int)System.Math.Ceiling((double)response.Meta.Total / response.Meta.Limit);
                        
                        Log.Info("[UniverseSyncManager] Received {Count} systems from API (Total Expected: {Total}).", response.Data.Count, TotalSystemsExpected);

                        var existingSystems = _dbManager.GetAllSystems().ToDictionary(s => s.Symbol, s => s);

                        var indexed = response.Data.Select(s => {
                            var system = new DatabaseManager.IndexedSystem {
                                Symbol = s.Symbol,
                                SectorSymbol = s.SectorSymbol,
                                Type = s.Type.ToString(),
                                X = s.X,
                                Y = s.Y,
                                WaypointCount = s.Waypoints != null ? s.Waypoints.Count : 0
                            };
                            
                            // Preserve known facilities if they exist in the DB
                            if (existingSystems.TryGetValue(s.Symbol, out var existing))
                            {
                                system.KnownFacilities = existing.KnownFacilities;
                            }
                            
                            return system;
                        }).ToList();

                        _dbManager.StoreSystems(indexed);

                        // Extract JUMP_GATE waypoints from each system and register them for Phase 2.
                        var jumpGateWaypoints = response.Data
                            .SelectMany(s => (s.Waypoints ?? Enumerable.Empty<SystemWaypoint>())
                                .Where(w => w.Type == WaypointType.JUMPGATE)
                                .Select(w => (w.Symbol, s.Symbol)))
                            .ToList();

                        if (jumpGateWaypoints.Count > 0)
                        {
                            _dbManager.StoreJumpGateWaypoints(jumpGateWaypoints);
                            Log.Info("[UniverseSyncManager] Registered {Count} JUMP_GATE waypoints for connection sync.", jumpGateWaypoints.Count);
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
}
