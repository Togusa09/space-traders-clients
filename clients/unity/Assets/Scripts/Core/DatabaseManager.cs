using System;
using System.IO;
using System.Collections.Generic;
using SQLite;
using UnityEngine;

namespace SpaceTraders.Core
{
    public class DatabaseManager : MonoBehaviour
    {
        private static DatabaseManager _instance;
        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<DatabaseManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DatabaseManager");
                        _instance = go.AddComponent<DatabaseManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private string _dbPath;
        private SQLiteConnection _db;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            _dbPath = Path.Combine(Application.persistentDataPath, "spacetraders_v2.db");
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                string dir = Path.GetDirectoryName(_dbPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                Debug.Log($"[DatabaseManager] Connecting to {_dbPath}");
                _db = new SQLiteConnection(_dbPath);
                
                // Use ExecuteScalar for pragmas that return values to avoid "not an error" exceptions
                try { _db.ExecuteScalar<string>("PRAGMA journal_mode = WAL"); } catch { }
                try { _db.ExecuteScalar<int>("PRAGMA synchronous = OFF"); } catch { }
                
                // Create tables
                _db.CreateTable<ApiCacheEntry>();
                _db.CreateTable<IndexedSystem>();
                
                Debug.Log($"[DatabaseManager] SQLite initialized successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] Critical failure during initialization: {e.Message}\n{e.StackTrace}");
                _db = null;
            }
        }

        public void SetCache(string key, string json)
        {
            if (_db == null) return;
            try
            {
                var entry = new ApiCacheEntry
                {
                    CacheKey = key,
                    JsonData = json,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                _db.InsertOrReplace(entry);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] SetCache failed: {e.Message}");
            }
        }

        public string GetCache(string key, long maxAgeSeconds)
        {
            if (_db == null) return null;
            try
            {
                var entry = _db.Table<ApiCacheEntry>().Where(x => x.CacheKey == key).FirstOrDefault();
                if (entry != null)
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    if (now - entry.Timestamp < maxAgeSeconds)
                    {
                        return entry.JsonData;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DatabaseManager] GetCache failed: {e.Message}");
            }
            return null;
        }

        public void ClearCache()
        {
            if (_db == null) return;
            try
            {
                _db.DeleteAll<ApiCacheEntry>();
                _db.DeleteAll<IndexedSystem>();
                Debug.Log("[DatabaseManager] All cached data cleared.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] ClearCache failed: {e.Message}");
            }
        }

        // --- Efficient Universe Querying ---

        public void StoreSystems(IEnumerable<IndexedSystem> systems)
        {
            if (_db == null) return;
            try
            {
                _db.RunInTransaction(() => {
                    foreach (var s in systems) _db.InsertOrReplace(s);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] StoreSystems failed: {e.Message}");
            }
        }

        public List<IndexedSystem> SearchSystems(string symbolPattern)
        {
            if (_db == null) return new List<IndexedSystem>();
            try 
            {
                var query = _db.Table<IndexedSystem>();
                if (!string.IsNullOrEmpty(symbolPattern))
                {
                    query = query.Where(s => s.Symbol.Contains(symbolPattern));
                }
                return query.Take(100).ToList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] Search failed: {e.Message}");
                return new List<IndexedSystem>();
            }
        }

        public int GetIndexedSystemCount()
        {
            if (_db == null) return 0;
            try { return _db.Table<IndexedSystem>().Count(); }
            catch { return 0; }
        }

        private void OnDestroy()
        {
            _db?.Close();
        }

        [Table("api_cache")]
        public class ApiCacheEntry
        {
            [PrimaryKey]
            public string CacheKey { get; set; }
            public string JsonData { get; set; }
            public long Timestamp { get; set; }
        }

        [Table("systems_index")]
        public class IndexedSystem
        {
            [PrimaryKey]
            public string Symbol { get; set; }
            public string SectorSymbol { get; set; }
            public string Type { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int WaypointCount { get; set; }
        }
    }
}
