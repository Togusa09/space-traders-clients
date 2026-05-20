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

        public SQLiteConnection Connection
        {
            get
            {
                if (_db == null)
                {
                    if (string.IsNullOrEmpty(_dbPath))
                    {
                        _dbPath = Path.Combine(Application.persistentDataPath, "spacetraders_v2.db");
                    }
                    InitializeDatabase();
                }
                return _db;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (string.IsNullOrEmpty(_dbPath))
            {
                _dbPath = Path.Combine(Application.persistentDataPath, "spacetraders_v2.db");
            }
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
                try { _db.ExecuteScalar<int>("PRAGMA synchronous = NORMAL"); } catch { }
                
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
            var db = Connection;
            if (db == null) return;
            try
            {
                var entry = new ApiCacheEntry
                {
                    CacheKey = key,
                    JsonData = json,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                db.InsertOrReplace(entry);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] SetCache failed: {e.Message}");
            }
        }

        public string GetCache(string key, long maxAgeSeconds)
        {
            var db = Connection;
            if (db == null) return null;
            try
            {
                var entry = db.Table<ApiCacheEntry>().Where(x => x.CacheKey == key).FirstOrDefault();
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
            var db = Connection;
            if (db == null) return;
            try
            {
                db.DeleteAll<ApiCacheEntry>();
                db.DeleteAll<IndexedSystem>();
                Debug.Log("[DatabaseManager] All cached data cleared.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] ClearCache failed: {e.Message}");
            }
        }

        // --- Efficient Universe Querying ---

        public List<IndexedSystem> GetAllSystems()
        {
            var db = Connection;
            if (db == null) return new List<IndexedSystem>();
            try { return db.Table<IndexedSystem>().ToList(); }
            catch { return new List<IndexedSystem>(); }
        }

        public void StoreSystems(IEnumerable<IndexedSystem> systems)
        {
            var db = Connection;
            if (db == null) return;
            try
            {
                db.RunInTransaction(() => {
                    foreach (var s in systems) db.InsertOrReplace(s);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[DatabaseManager] StoreSystems failed: {e.Message}");
            }
        }

        public List<IndexedSystem> SearchSystems(string symbolPattern)
        {
            var db = Connection;
            if (db == null) return new List<IndexedSystem>();
            try 
            {
                var query = db.Table<IndexedSystem>();
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
            var db = Connection;
            if (db == null) return 0;
            try { return db.Table<IndexedSystem>().Count(); }
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
