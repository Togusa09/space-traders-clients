using System;
using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SpaceTraders.Core;

namespace SpaceTraders.Tests
{
    [TestFixture]
    public class DatabaseManagerTests
    {
        private string _dbPath;
        private DatabaseManager _dbManager;
        private GameObject _dbGo;

        [SetUp]
        public void Setup()
        {
            string testDir = Path.Combine(Path.GetTempPath(), "SpaceTradersTests", Guid.NewGuid().ToString("N"));
            _dbPath = Path.Combine(testDir, "spacetraders_test.db");
            Environment.SetEnvironmentVariable("SPACETRADERS_DB_PATH", _dbPath);

            // Create a fresh instance of DatabaseManager
            _dbGo = new GameObject("TestDatabaseManager");
            _dbManager = _dbGo.AddComponent<DatabaseManager>();
            _dbManager.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            // Close database and destroy GameObject
            if (_dbGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_dbGo);
            }

            Environment.SetEnvironmentVariable("SPACETRADERS_DB_PATH", null);

            // Delete isolated test database files/directories.
            try
            {
                if (!string.IsNullOrEmpty(_dbPath))
                {
                    if (File.Exists(_dbPath)) File.Delete(_dbPath);

                    string testDir = Path.GetDirectoryName(_dbPath);
                    if (!string.IsNullOrEmpty(testDir) && Directory.Exists(testDir))
                    {
                        Directory.Delete(testDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManagerTests] Test DB cleanup failed: {ex.Message}");
            }
        }

        [Test]
        public void SetAndGetCache_Success()
        {
            string key = "test_endpoint_key";
            string testJson = "{\"status\": \"ok\", \"value\": 42}";

            _dbManager.SetCache(key, testJson);

            // Retrieve immediately (max age 60 seconds)
            string retrieved = _dbManager.GetCache(key, 60);
            Assert.AreEqual(testJson, retrieved);
        }

        [Test]
        public void GetCache_Expired_ReturnsNull()
        {
            string key = "expired_key";
            string testJson = "{\"data\": \"old\"}";

            _dbManager.SetCache(key, testJson);

            // Retrieve with max age of 0 seconds (expired instantly)
            string retrieved = _dbManager.GetCache(key, 0);
            Assert.IsNull(retrieved);
        }

        [Test]
        public void ClearCache_EmptiesAllTables()
        {
            _dbManager.SetCache("key1", "data1");
            
            var testSystem = new DatabaseManager.IndexedSystem
            {
                Symbol = "X1-Y2",
                SectorSymbol = "X1",
                Type = "RED_STAR",
                X = 10,
                Y = -20,
                WaypointCount = 5
            };
            _dbManager.StoreSystems(new List<DatabaseManager.IndexedSystem> { testSystem });

            Assert.AreEqual(1, _dbManager.GetIndexedSystemCount());
            Assert.AreEqual("data1", _dbManager.GetCache("key1", 100));

            // Clear cache
            _dbManager.ClearCache();

            Assert.AreEqual(0, _dbManager.GetIndexedSystemCount());
            Assert.IsNull(_dbManager.GetCache("key1", 100));
        }

        [Test]
        public void StoreAndQuerySystems_BulkTransaction_Success()
        {
            var systems = new List<DatabaseManager.IndexedSystem>
            {
                new DatabaseManager.IndexedSystem { Symbol = "SOL-A", SectorSymbol = "SOL", Type = "YELLOW_STAR", X = 0, Y = 0, WaypointCount = 9 },
                new DatabaseManager.IndexedSystem { Symbol = "SOL-B", SectorSymbol = "SOL", Type = "RED_STAR", X = 10, Y = 5, WaypointCount = 3 },
                new DatabaseManager.IndexedSystem { Symbol = "VEGA-I", SectorSymbol = "VEGA", Type = "BLUE_STAR", X = -50, Y = 100, WaypointCount = 12 }
            };

            _dbManager.StoreSystems(systems);

            // Verify count
            Assert.AreEqual(3, _dbManager.GetIndexedSystemCount());

            // Get all
            var all = _dbManager.GetAllSystems();
            Assert.AreEqual(3, all.Count);
            Assert.IsTrue(all.Exists(s => s.Symbol == "VEGA-I"));

            // Search by query
            var solSystems = _dbManager.SearchSystems("SOL");
            Assert.AreEqual(2, solSystems.Count);
            Assert.IsTrue(solSystems.Exists(s => s.Symbol == "SOL-A"));
            Assert.IsTrue(solSystems.Exists(s => s.Symbol == "SOL-B"));
            Assert.IsFalse(solSystems.Exists(s => s.Symbol == "VEGA-I"));
        }
    }
}
