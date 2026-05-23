using System;
using System.IO;
using System.Collections.Generic;
using SQLite;
using UnityEngine;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.Core
{
    public class DatabaseManager : MonoBehaviour, IApiCacheRepository, ISystemIndexRepository, IJumpGateRepository
    {
        private const string DefaultDatabaseFileName = "spacetraders_v2.db";
        private const string DatabasePathOverrideEnvVar = "SPACETRADERS_DB_PATH";

        // Global override primarily for tests so they can isolate DB files.
        public static string GlobalDatabasePathOverride { get; set; }

        [SerializeField] private string _databasePathOverride;
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
                        _dbPath = ResolveDatabasePath();
                    }
                    InitializeDatabase();
                }
                return _db;
            }
        }

        public static string BuildDefaultDatabasePath()
        {
            return Path.Combine(Application.persistentDataPath, DefaultDatabaseFileName);
        }

        private string ResolveDatabasePath()
        {
            if (!string.IsNullOrEmpty(_databasePathOverride))
            {
                return _databasePathOverride;
            }

            string environmentOverridePath = Environment.GetEnvironmentVariable(DatabasePathOverrideEnvVar);
            if (!string.IsNullOrEmpty(environmentOverridePath))
            {
                return environmentOverridePath;
            }

            if (!string.IsNullOrEmpty(GlobalDatabasePathOverride))
            {
                return GlobalDatabasePathOverride;
            }

            return BuildDefaultDatabasePath();
        }

        private void Awake()
        {
            if (string.IsNullOrEmpty(_dbPath))
            {
                _dbPath = ResolveDatabasePath();
            }
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                string dir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                Log.Info("[DatabaseManager] Connecting to {Path}", _dbPath);
                _db = new SQLiteConnection(_dbPath);
                
                // Use ExecuteScalar for pragmas that return values to avoid "not an error" exceptions
                try { _db.ExecuteScalar<string>("PRAGMA journal_mode = WAL"); } catch { }
                try { _db.ExecuteScalar<int>("PRAGMA synchronous = NORMAL"); } catch { }
                
                // Create tables
                _db.CreateTable<ApiCacheEntry>();
                _db.CreateTable<IndexedSystem>();
                _db.CreateTable<IndexedJumpGate>();
                
                Log.Info("[DatabaseManager] SQLite initialized successfully.");
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] Critical failure during initialization: {Error}\n{StackTrace}", e.Message, e.StackTrace);
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
                Log.Error("[DatabaseManager] SetCache failed: {Error}", e.Message);
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
                Log.Warning("[DatabaseManager] GetCache failed: {Error}", e.Message);
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
                db.DeleteAll<IndexedJumpGate>();
                Log.Info("[DatabaseManager] All cached data cleared.");
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] ClearCache failed: {Error}", e.Message);
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
                Log.Error("[DatabaseManager] StoreSystems failed: {Error}", e.Message);
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
                Log.Error("[DatabaseManager] Search failed: {Error}", e.Message);
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

        // --- Jump Gate Index ---

        /// <summary>
        /// Stores JUMP_GATE waypoint stubs (no connections yet). Uses InsertOrIgnore so
        /// already-fetched records are never overwritten.
        /// </summary>
        public void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints)
        {
            var db = Connection;
            if (db == null) return;
            try
            {
                db.RunInTransaction(() =>
                {
                    foreach (var (wp, sys) in waypoints)
                    {
                        db.Insert(new IndexedJumpGate
                        {
                            WaypointSymbol = wp,
                            SystemSymbol = sys,
                            ConnectionsJson = null
                        }, "OR IGNORE");
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] StoreJumpGateWaypoints failed: {Error}", e.Message);
            }
        }

        /// <summary>
        /// Persists the fetched connection list for a given JUMP_GATE waypoint.
        /// </summary>
        public void StoreJumpGateConnections(string waypointSymbol, List<string> connections)
        {
            var db = Connection;
            if (db == null) return;
            try
            {
                string json = connections != null
                    ? string.Join(",", connections)
                    : string.Empty;
                db.Execute(
                    "UPDATE jump_gate_index SET ConnectionsJson = ? WHERE WaypointSymbol = ?",
                    json, waypointSymbol);
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] StoreJumpGateConnections failed: {Error}", e.Message);
            }
        }

        /// <summary>
        /// Returns waypoints whose connection data has not yet been fetched.
        /// </summary>
        public List<IndexedJumpGate> GetPendingJumpGates()
        {
            var db = Connection;
            if (db == null) return new List<IndexedJumpGate>();
            try
            {
                return db.Table<IndexedJumpGate>()
                    .Where(j => j.ConnectionsJson == null)
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] GetPendingJumpGates failed: {Error}", e.Message);
                return new List<IndexedJumpGate>();
            }
        }

        /// <summary>
        /// Returns all jump gate entries that have fetched connection data.
        /// </summary>
        public List<IndexedJumpGate> GetAllJumpGateConnections()
        {
            var db = Connection;
            if (db == null) return new List<IndexedJumpGate>();
            try
            {
                return db.Table<IndexedJumpGate>()
                    .Where(j => j.ConnectionsJson != null)
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] GetAllJumpGateConnections failed: {Error}", e.Message);
                return new List<IndexedJumpGate>();
            }
        }

        public int GetIndexedJumpGateCount()
        {
            var db = Connection;
            if (db == null) return 0;
            try { return db.Table<IndexedJumpGate>().Count(); }
            catch { return 0; }
        }

        /// <summary>
        /// Closes the active SQLite connection if one exists.
        /// Primarily intended for deterministic teardown in tests.
        /// </summary>
        public void CloseConnection()
        {
            if (_db == null) return;

            try
            {
                _db.Close();
            }
            catch (Exception e)
            {
                Log.Warning("[DatabaseManager] CloseConnection failed: {Error}", e.Message);
            }
            finally
            {
                _db = null;
            }
        }

        private void OnDestroy()
        {
            CloseConnection();
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
            public string KnownFacilities { get; set; }
        }

        [Table("jump_gate_index")]
        public class IndexedJumpGate
        {
            [PrimaryKey]
            public string WaypointSymbol { get; set; }
            public string SystemSymbol { get; set; }
            /// <summary>
            /// Comma-separated list of connected waypoint symbols, or null if not yet fetched.
            /// </summary>
            public string ConnectionsJson { get; set; }
        }
    }

    internal interface IApiCacheRepository
    {
        void SetCache(string key, string json);
        string GetCache(string key, long maxAgeSeconds);
        void ClearCache();
    }

    internal interface ISystemIndexRepository
    {
        List<DatabaseManager.IndexedSystem> GetAllSystems();
        void StoreSystems(IEnumerable<DatabaseManager.IndexedSystem> systems);
        List<DatabaseManager.IndexedSystem> SearchSystems(string symbolPattern);
        int GetIndexedSystemCount();
    }

    internal interface IJumpGateRepository
    {
        void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints);
        void StoreJumpGateConnections(string waypointSymbol, List<string> connections);
        List<DatabaseManager.IndexedJumpGate> GetPendingJumpGates();
        List<DatabaseManager.IndexedJumpGate> GetAllJumpGateConnections();
        int GetIndexedJumpGateCount();
    }
}
