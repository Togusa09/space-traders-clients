using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SpaceTraders.Core;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpaceTraders.Tests.PlayMode
{
    public class DatabaseManagerPlayModeTests
    {
        private string _dbPath;
        private GameObject _dbObject;
        private DatabaseManager _databaseManager;

        [SetUp]
        public void SetUp()
        {
            string testDirectory = Path.Combine(Path.GetTempPath(), "SpaceTradersPlayModeTests", Guid.NewGuid().ToString("N"));
            _dbPath = Path.Combine(testDirectory, "spacetraders_playmode_test.db");
            Environment.SetEnvironmentVariable("SPACETRADERS_DB_PATH", _dbPath);

            _dbObject = new GameObject("PlayModeDatabaseManager");
            _databaseManager = _dbObject.AddComponent<DatabaseManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_databaseManager != null)
            {
                _databaseManager.CloseConnection();
            }

            if (_dbObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_dbObject);
            }

            Environment.SetEnvironmentVariable("SPACETRADERS_DB_PATH", null);

            try
            {
                if (!string.IsNullOrEmpty(_dbPath))
                {
                    if (File.Exists(_dbPath))
                    {
                        File.Delete(_dbPath);
                    }

                    string testDirectory = Path.GetDirectoryName(_dbPath);
                    if (!string.IsNullOrEmpty(testDirectory) && Directory.Exists(testDirectory))
                    {
                        Directory.Delete(testDirectory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManagerPlayModeTests] Cleanup failed: {ex.Message}");
            }
        }

        [UnityTest]
        public IEnumerator Awake_InitializesConnection_AndCacheRoundTripWorks()
        {
            yield return null;

            _databaseManager.SetCache("playmode-cache-key", "{\"value\":42}");
            string cached = _databaseManager.GetCache("playmode-cache-key", 60);

            Assert.AreEqual("{\"value\":42}", cached);
        }

        [UnityTest]
        public IEnumerator ClearCache_RemovesIndexedSystemsAndJumpGates()
        {
            yield return null;

            _databaseManager.StoreSystems(new List<IndexedSystem>
            {
                new IndexedSystem { Symbol = "X1-TEST", SectorSymbol = "X1", Type = "NEUTRON_STAR", X = 0, Y = 0, WaypointCount = 1 }
            });
            _databaseManager.StoreJumpGateWaypoints(new List<(string, string)> { ("X1-TEST-JG", "X1-TEST") });

            Assert.AreEqual(1, _databaseManager.GetIndexedSystemCount());
            Assert.AreEqual(1, _databaseManager.GetIndexedJumpGateCount());

            _databaseManager.ClearCache();

            Assert.AreEqual(0, _databaseManager.GetIndexedSystemCount());
            Assert.AreEqual(0, _databaseManager.GetIndexedJumpGateCount());
        }
    }
}
