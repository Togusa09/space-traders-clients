using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceTraders.Core;
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
            var jumpGateRepository = new FakeJumpGateRepository(indexedJumpGateCount: 5, pendingCount: 2);

            syncManager.Construct(apiService: null, authManager, systemsRepository, jumpGateRepository);

            // Allow Unity Start() to run.
            yield return null;

            Assert.AreEqual(5, syncManager.TotalJumpGates);
            Assert.AreEqual(2, syncManager.PendingJumpGates);
            Assert.IsFalse(syncManager.IsSyncing);
            Assert.AreEqual(SyncPhase.Idle, syncManager.CurrentPhase);

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

        private sealed class FakeSystemIndexRepository : ISystemIndexRepository
        {
            private readonly int _indexedSystemCount;

            public FakeSystemIndexRepository(int indexedSystemCount)
            {
                _indexedSystemCount = indexedSystemCount;
            }

            public List<IndexedSystem> GetAllSystems()
            {
                return new List<IndexedSystem>();
            }

            public void StoreSystems(IEnumerable<IndexedSystem> systems)
            {
            }

            public List<IndexedSystem> SearchSystems(string symbolPattern)
            {
                return new List<IndexedSystem>();
            }

            public int GetIndexedSystemCount()
            {
                return _indexedSystemCount;
            }
        }

        private sealed class FakeJumpGateRepository : IJumpGateRepository
        {
            private readonly int _indexedJumpGateCount;
            private readonly int _pendingCount;

            public FakeJumpGateRepository(int indexedJumpGateCount, int pendingCount)
            {
                _indexedJumpGateCount = indexedJumpGateCount;
                _pendingCount = pendingCount;
            }

            public void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints)
            {
            }

            public void StoreJumpGateConnections(string waypointSymbol, List<string> connections)
            {
            }

            public List<IndexedJumpGate> GetPendingJumpGates()
            {
                var pending = new List<IndexedJumpGate>();
                for (int i = 0; i < _pendingCount; i++)
                {
                    pending.Add(new IndexedJumpGate { WaypointSymbol = $"X1-TEST-JG-{i}", SystemSymbol = "X1-TEST" });
                }

                return pending;
            }

            public List<IndexedJumpGate> GetAllJumpGateConnections()
            {
                return new List<IndexedJumpGate>();
            }

            public int GetIndexedJumpGateCount()
            {
                return _indexedJumpGateCount;
            }
        }
    }
}
