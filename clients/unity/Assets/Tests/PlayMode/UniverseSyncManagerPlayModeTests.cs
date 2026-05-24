using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpaceTraders.Tests.PlayMode
{
    public class UniverseSyncManagerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Start_WithInjectedDependencies_RefreshesCountsWithoutStartingSyncWhenNoToken()
        {
            var authObject = new GameObject("AuthManagerPlayMode");
            var authManager = authObject.AddComponent<AuthManager>();
            authManager.ClearTokens();

            var syncObject = new GameObject("UniverseSyncManagerPlayMode");
            var syncManager = syncObject.AddComponent<UniverseSyncManager>();

            var systemsRepository = new FakeSystemIndexRepository(indexedSystemCount: 7);
            var jumpGateRepository = new FakeJumpGateRepository(indexedJumpGateCount: 5, initialPendingCount: 2);

            syncManager.Construct(universeApiService: null, authManager, systemsRepository, jumpGateRepository);

            // Allow Unity Start() to run.
            yield return null;

            Assert.AreEqual(5, syncManager.TotalJumpGates);
            Assert.AreEqual(2, syncManager.PendingJumpGates);
            Assert.IsFalse(syncManager.IsSyncing);
            Assert.AreEqual(SyncPhase.Idle, syncManager.CurrentPhase);

            Object.DestroyImmediate(syncObject);
            Object.DestroyImmediate(authObject);
        }

        [UnityTest]
        public IEnumerator StartSync_WithHappyPath_CompletesAndStoresConnections()
        {
            var authObject = new GameObject("AuthManagerPlayMode");
            var authManager = authObject.AddComponent<AuthManager>();
            authManager.ClearTokens();

            var syncObject = new GameObject("UniverseSyncManagerPlayMode");
            var syncManager = syncObject.AddComponent<UniverseSyncManager>();

            var systemsRepository = new FakeSystemIndexRepository(indexedSystemCount: 0);
            var jumpGateRepository = new FakeJumpGateRepository(indexedJumpGateCount: 0, initialPendingCount: 0);
            var apiService = new FakeUniverseApiService();

            apiService.SystemsResponses.Enqueue(CreateSystemsResponseWithJumpGate("X1-TEST", "X1-TEST-JG"));
            apiService.JumpGateConnections["X1-TEST-JG"] = new List<string> { "X1-TEST-JG-2" };

            syncManager.Construct(apiService, authManager, systemsRepository, jumpGateRepository);

            syncManager.StartSync();
            yield return WaitForSyncToFinish(syncManager, 120);

            Assert.IsFalse(syncManager.IsSyncing);
            Assert.AreEqual(SyncPhase.Complete, syncManager.CurrentPhase);
            Assert.AreEqual(1f, syncManager.Progress);
            Assert.IsTrue(jumpGateRepository.ConnectionByWaypoint.ContainsKey("X1-TEST-JG"));
            Assert.AreEqual("X1-TEST-JG-2", jumpGateRepository.ConnectionByWaypoint["X1-TEST-JG"][0]);

            Object.DestroyImmediate(syncObject);
            Object.DestroyImmediate(authObject);
        }

        [UnityTest]
        public IEnumerator StartSync_WhenJumpGateFetchFails_StoresEmptyConnectionsAndCompletes()
        {
            var authObject = new GameObject("AuthManagerPlayMode");
            var authManager = authObject.AddComponent<AuthManager>();
            authManager.ClearTokens();

            var syncObject = new GameObject("UniverseSyncManagerPlayMode");
            var syncManager = syncObject.AddComponent<UniverseSyncManager>();

            var systemsRepository = new FakeSystemIndexRepository(indexedSystemCount: 0);
            var jumpGateRepository = new FakeJumpGateRepository(indexedJumpGateCount: 0, initialPendingCount: 0);
            var apiService = new FakeUniverseApiService();

            apiService.SystemsResponses.Enqueue(CreateSystemsResponseWithJumpGate("X1-FAIL", "X1-FAIL-JG"));
            apiService.ThrowOnJumpGateSymbols.Add("X1-FAIL-JG");

            syncManager.Construct(apiService, authManager, systemsRepository, jumpGateRepository);

            syncManager.StartSync();
            yield return WaitForSyncToFinish(syncManager, 120);

            Assert.IsFalse(syncManager.IsSyncing);
            Assert.AreEqual(SyncPhase.Complete, syncManager.CurrentPhase);
            Assert.IsTrue(jumpGateRepository.ConnectionByWaypoint.ContainsKey("X1-FAIL-JG"));
            Assert.AreEqual(0, jumpGateRepository.ConnectionByWaypoint["X1-FAIL-JG"].Count);

            Object.DestroyImmediate(syncObject);
            Object.DestroyImmediate(authObject);
        }

        [Test]
        public void StopSync_WhenIdle_IsSafeNoOp()
        {
            var syncObject = new GameObject("UniverseSyncManagerPlayMode");
            var syncManager = syncObject.AddComponent<UniverseSyncManager>();

            syncManager.StopSync();

            Assert.IsFalse(syncManager.IsSyncing);
            Assert.AreEqual(SyncPhase.Idle, syncManager.CurrentPhase);

            Object.DestroyImmediate(syncObject);
        }

        [UnityTest]
        public IEnumerator StopSync_DuringActiveSync_IsIdempotentAndReturnsToIdle()
        {
            var authObject = new GameObject("AuthManagerPlayMode");
            var authManager = authObject.AddComponent<AuthManager>();
            authManager.ClearTokens();

            var syncObject = new GameObject("UniverseSyncManagerPlayMode");
            var syncManager = syncObject.AddComponent<UniverseSyncManager>();

            var systemsRepository = new FakeSystemIndexRepository(indexedSystemCount: 0);
            var jumpGateRepository = new FakeJumpGateRepository(indexedJumpGateCount: 0, initialPendingCount: 0);
            var apiService = new FakeUniverseApiService { DelaySystemsResponseMs = 250 };
            apiService.SystemsResponses.Enqueue(CreateSystemsResponseWithJumpGate("X1-SLOW", "X1-SLOW-JG"));

            syncManager.Construct(apiService, authManager, systemsRepository, jumpGateRepository);

            syncManager.StartSync();
            yield return null;

            syncManager.StopSync();
            syncManager.StopSync();

            Assert.IsFalse(syncManager.IsSyncing);
            Assert.AreEqual(SyncPhase.Idle, syncManager.CurrentPhase);

            Object.DestroyImmediate(syncObject);
            Object.DestroyImmediate(authObject);
        }

        private static IEnumerator WaitForSyncToFinish(UniverseSyncManager manager, int maxFrames)
        {
            int remaining = maxFrames;
            while (manager.IsSyncing && remaining > 0)
            {
                remaining--;
                yield return null;
            }
            Assert.Greater(remaining, 0, "Timed out waiting for UniverseSyncManager to finish sync.");
        }

        private static GetSystems200Response CreateSystemsResponseWithJumpGate(string systemSymbol, string jumpGateWaypointSymbol)
        {
            var system = new SpaceTraders.Generated.Model.System(
                symbol: systemSymbol,
                sectorSymbol: "X1",
                type: SystemType.NEUTRONSTAR,
                x: 0,
                y: 0,
                waypoints: new List<SystemWaypoint>
                {
                    new SystemWaypoint(jumpGateWaypointSymbol, WaypointType.JUMPGATE, 10, 20, new List<WaypointOrbital>())
                },
                factions: new List<SystemFaction>());

            return new GetSystems200Response(
                data: new List<SpaceTraders.Generated.Model.System> { system },
                meta: new Meta(total: 1, page: 1, limit: 20));
        }

        private sealed class FakeSystemIndexRepository : ISystemIndexRepository
        {
            private readonly List<IndexedSystem> _systems = new List<IndexedSystem>();

            public FakeSystemIndexRepository(int indexedSystemCount)
            {
                for (int i = 0; i < indexedSystemCount; i++)
                {
                    _systems.Add(new IndexedSystem { Symbol = $"X1-EXISTING-{i}" });
                }
            }

            public List<IndexedSystem> GetAllSystems()
            {
                return new List<IndexedSystem>(_systems);
            }

            public void StoreSystems(IEnumerable<IndexedSystem> systems)
            {
                foreach (var system in systems)
                {
                    var existingIndex = _systems.FindIndex(s => s.Symbol == system.Symbol);
                    if (existingIndex >= 0)
                    {
                        _systems[existingIndex] = system;
                    }
                    else
                    {
                        _systems.Add(system);
                    }
                }
            }

            public List<IndexedSystem> SearchSystems(string symbolPattern)
            {
                return new List<IndexedSystem>();
            }

            public int GetIndexedSystemCount()
            {
                return _systems.Count;
            }
        }

        private sealed class FakeJumpGateRepository : IJumpGateRepository
        {
            private readonly List<IndexedJumpGate> _gates = new List<IndexedJumpGate>();

            public Dictionary<string, List<string>> ConnectionByWaypoint { get; } = new Dictionary<string, List<string>>();

            public FakeJumpGateRepository(int indexedJumpGateCount, int initialPendingCount)
            {
                for (int i = 0; i < indexedJumpGateCount; i++)
                {
                    _gates.Add(new IndexedJumpGate
                    {
                        WaypointSymbol = $"X1-INDEXED-JG-{i}",
                        SystemSymbol = "X1-INDEXED",
                        ConnectionsJson = i < initialPendingCount ? null : string.Empty
                    });
                }
            }

            public void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints)
            {
                foreach (var (waypointSymbol, systemSymbol) in waypoints)
                {
                    if (_gates.Exists(g => g.WaypointSymbol == waypointSymbol))
                    {
                        continue;
                    }

                    _gates.Add(new IndexedJumpGate
                    {
                        WaypointSymbol = waypointSymbol,
                        SystemSymbol = systemSymbol,
                        ConnectionsJson = null
                    });
                }
            }

            public void StoreJumpGateConnections(string waypointSymbol, List<string> connections)
            {
                ConnectionByWaypoint[waypointSymbol] = connections ?? new List<string>();

                var gate = _gates.Find(g => g.WaypointSymbol == waypointSymbol);
                if (gate != null)
                {
                    gate.ConnectionsJson = string.Join(",", ConnectionByWaypoint[waypointSymbol]);
                }
            }

            public List<IndexedJumpGate> GetPendingJumpGates()
            {
                return _gates.FindAll(g => g.ConnectionsJson == null);
            }

            public List<IndexedJumpGate> GetAllJumpGateConnections()
            {
                return _gates.FindAll(g => g.ConnectionsJson != null);
            }

            public int GetIndexedJumpGateCount()
            {
                return _gates.Count;
            }
        }

        private sealed class FakeUniverseApiService : IUniverseApiService
        {
            public Queue<GetSystems200Response> SystemsResponses { get; } = new Queue<GetSystems200Response>();
            public Dictionary<string, List<string>> JumpGateConnections { get; } = new Dictionary<string, List<string>>();
            public HashSet<string> ThrowOnJumpGateSymbols { get; } = new HashSet<string>();
            public int DelaySystemsResponseMs { get; set; }

            public async Task<GetSystems200Response> GetSystems(int page = 1, int limit = 10)
            {
                if (DelaySystemsResponseMs > 0)
                {
                    await Task.Delay(DelaySystemsResponseMs);
                }

                if (SystemsResponses.Count > 0)
                {
                    return SystemsResponses.Dequeue();
                }

                return new GetSystems200Response(new List<SpaceTraders.Generated.Model.System>(), new Meta(total: 0, page: page, limit: limit));
            }

            public Task<GetJumpGate200Response> GetJumpGate(string systemSymbol, string waypointSymbol)
            {
                if (ThrowOnJumpGateSymbols.Contains(waypointSymbol))
                {
                    throw new System.Exception("Simulated jump gate failure.");
                }

                if (!JumpGateConnections.TryGetValue(waypointSymbol, out var connections))
                {
                    connections = new List<string>();
                }

                return Task.FromResult(new GetJumpGate200Response(new JumpGate(waypointSymbol, connections)));
            }
        }
    }
}
