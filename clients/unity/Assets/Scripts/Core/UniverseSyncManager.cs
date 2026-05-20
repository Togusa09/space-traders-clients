using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpaceTraders.API;
using SpaceTraders.API.Models;
using VContainer;

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
            int existingCount = _dbManager.GetIndexedSystemCount();
            Debug.Log($"[UniverseSyncManager] Initial check. Indexed systems: {existingCount}");
            
            if (existingCount == 0)
            {
                Debug.Log("[UniverseSyncManager] DB empty, auto-starting sync...");
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
            Debug.Log("[UniverseSyncManager] Sync cancel requested.");
        }

        private async Task SyncUniverseAsync(CancellationToken token)
        {
            _isSyncing = true;
            Progress = 0f;
            Debug.Log("[UniverseSyncManager] Starting async background universe sync...");

            CurrentPage = 1;
            TotalPages = 1;
            const int limit = 20;

            try
            {
                do
                {
                    if (token.IsCancellationRequested) break;

                    Debug.Log($"[UniverseSyncManager] Requesting page {CurrentPage}...");
                    SystemsResponse response = await _apiService.GetSystems(CurrentPage, limit);

                    if (response != null && response.data != null)
                    {
                        TotalSystemsExpected = response.meta.total;
                        TotalPages = (int)System.Math.Ceiling((double)response.meta.total / response.meta.limit);
                        
                        Debug.Log($"[UniverseSyncManager] Received {response.data.Length} systems from API (Total Expected: {TotalSystemsExpected}).");

                        var indexed = response.data.Select(s => new DatabaseManager.IndexedSystem {
                            Symbol = s.symbol,
                            SectorSymbol = s.sectorSymbol,
                            Type = s.type,
                            X = s.x,
                            Y = s.y,
                            WaypointCount = s.waypoints != null ? s.waypoints.Length : 0
                        }).ToList();

                        _dbManager.StoreSystems(indexed);
                        
                        int newCount = _dbManager.GetIndexedSystemCount();
                        Debug.Log($"[UniverseSyncManager] Page {CurrentPage} stored. Total indexed: {newCount}");

                        Progress = (float)CurrentPage / TotalPages;
                        CurrentPage++;
                        
                        // Respect Rate Limits
                        await Task.Delay(1100, token);
                    }
                    else
                    {
                        Debug.LogError($"[UniverseSyncManager] Sync failed on page {CurrentPage}: No data in response");
                        await Task.Delay(5000, token);
                    }

                } while (CurrentPage <= TotalPages);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UniverseSyncManager] Sync task cancelled.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UniverseSyncManager] Sync encountered a critical error: {e.Message}");
            }
            finally
            {
                _isSyncing = false;
                Debug.Log("[UniverseSyncManager] Universe sync process terminated.");
            }
        }
    }
}
