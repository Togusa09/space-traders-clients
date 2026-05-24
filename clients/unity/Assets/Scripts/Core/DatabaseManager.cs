using System;
using System.Collections.Generic;
using System.IO;
using SQLite;
using Unity.Logging;
using UnityEngine;

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

        private IApiCacheRepository _apiCacheRepository;
        private ISystemIndexRepository _systemIndexRepository;
        private IJumpGateRepository _jumpGateRepository;

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

        private IApiCacheRepository ApiCacheRepository
        {
            get
            {
                EnsureRepositoriesInitialized();
                return _apiCacheRepository;
            }
        }

        private ISystemIndexRepository SystemIndexRepository
        {
            get
            {
                EnsureRepositoriesInitialized();
                return _systemIndexRepository;
            }
        }

        private IJumpGateRepository JumpGateRepository
        {
            get
            {
                EnsureRepositoriesInitialized();
                return _jumpGateRepository;
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
            EnsureRepositoriesInitialized();
        }

        private void EnsureRepositoriesInitialized()
        {
            if (_apiCacheRepository == null)
            {
                _apiCacheRepository = new SqliteApiCacheRepository(() => Connection);
            }

            if (_systemIndexRepository == null)
            {
                _systemIndexRepository = new SqliteSystemIndexRepository(() => Connection);
            }

            if (_jumpGateRepository == null)
            {
                _jumpGateRepository = new SqliteJumpGateRepository(() => Connection);
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                string dir = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                Log.Info("[DatabaseManager] Connecting to {Path}", _dbPath);
                _db = new SQLiteConnection(_dbPath);

                // Use ExecuteScalar for pragmas that return values to avoid noisy sqlite exceptions.
                try { _db.ExecuteScalar<string>("PRAGMA journal_mode = WAL"); } catch { }
                try { _db.ExecuteScalar<int>("PRAGMA synchronous = NORMAL"); } catch { }

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
            ApiCacheRepository.SetCache(key, json);
        }

        public string GetCache(string key, long maxAgeSeconds)
        {
            return ApiCacheRepository.GetCache(key, maxAgeSeconds);
        }

        public void ClearCache()
        {
            ApiCacheRepository.ClearCache();
        }

        public List<IndexedSystem> GetAllSystems()
        {
            return SystemIndexRepository.GetAllSystems();
        }

        public void StoreSystems(IEnumerable<IndexedSystem> systems)
        {
            SystemIndexRepository.StoreSystems(systems);
        }

        public List<IndexedSystem> SearchSystems(string symbolPattern)
        {
            return SystemIndexRepository.SearchSystems(symbolPattern);
        }

        public int GetIndexedSystemCount()
        {
            return SystemIndexRepository.GetIndexedSystemCount();
        }

        public void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints)
        {
            JumpGateRepository.StoreJumpGateWaypoints(waypoints);
        }

        public void StoreJumpGateConnections(string waypointSymbol, List<string> connections)
        {
            JumpGateRepository.StoreJumpGateConnections(waypointSymbol, connections);
        }

        public List<IndexedJumpGate> GetPendingJumpGates()
        {
            return JumpGateRepository.GetPendingJumpGates();
        }

        public List<IndexedJumpGate> GetAllJumpGateConnections()
        {
            return JumpGateRepository.GetAllJumpGateConnections();
        }

        public int GetIndexedJumpGateCount()
        {
            return JumpGateRepository.GetIndexedJumpGateCount();
        }

        /// <summary>
        /// Closes the active SQLite connection if one exists.
        /// Primarily intended for deterministic teardown in tests.
        /// </summary>
        public void CloseConnection()
        {
            if (_db == null)
            {
                return;
            }

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

    internal sealed class SqliteApiCacheRepository : IApiCacheRepository
    {
        private readonly Func<SQLiteConnection> _getConnection;

        public SqliteApiCacheRepository(Func<SQLiteConnection> getConnection)
        {
            _getConnection = getConnection;
        }

        public void SetCache(string key, string json)
        {
            var db = _getConnection();
            if (db == null)
            {
                return;
            }

            try
            {
                var entry = new DatabaseManager.ApiCacheEntry
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
            var db = _getConnection();
            if (db == null)
            {
                return null;
            }

            try
            {
                var entry = db.Table<DatabaseManager.ApiCacheEntry>().Where(x => x.CacheKey == key).FirstOrDefault();
                if (entry == null)
                {
                    return null;
                }

                long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return now - entry.Timestamp < maxAgeSeconds ? entry.JsonData : null;
            }
            catch (Exception e)
            {
                Log.Warning("[DatabaseManager] GetCache failed: {Error}", e.Message);
                return null;
            }
        }

        public void ClearCache()
        {
            var db = _getConnection();
            if (db == null)
            {
                return;
            }

            try
            {
                db.DeleteAll<DatabaseManager.ApiCacheEntry>();
                db.DeleteAll<DatabaseManager.IndexedSystem>();
                db.DeleteAll<DatabaseManager.IndexedJumpGate>();
                Log.Info("[DatabaseManager] All cached data cleared.");
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] ClearCache failed: {Error}", e.Message);
            }
        }
    }

    internal sealed class SqliteSystemIndexRepository : ISystemIndexRepository
    {
        private readonly Func<SQLiteConnection> _getConnection;

        public SqliteSystemIndexRepository(Func<SQLiteConnection> getConnection)
        {
            _getConnection = getConnection;
        }

        public List<DatabaseManager.IndexedSystem> GetAllSystems()
        {
            var db = _getConnection();
            if (db == null)
            {
                return new List<DatabaseManager.IndexedSystem>();
            }

            try
            {
                return db.Table<DatabaseManager.IndexedSystem>().ToList();
            }
            catch
            {
                return new List<DatabaseManager.IndexedSystem>();
            }
        }

        public void StoreSystems(IEnumerable<DatabaseManager.IndexedSystem> systems)
        {
            var db = _getConnection();
            if (db == null)
            {
                return;
            }

            try
            {
                db.RunInTransaction(() =>
                {
                    foreach (var system in systems)
                    {
                        db.InsertOrReplace(system);
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] StoreSystems failed: {Error}", e.Message);
            }
        }

        public List<DatabaseManager.IndexedSystem> SearchSystems(string symbolPattern)
        {
            var db = _getConnection();
            if (db == null)
            {
                return new List<DatabaseManager.IndexedSystem>();
            }

            try
            {
                var query = db.Table<DatabaseManager.IndexedSystem>();
                if (!string.IsNullOrEmpty(symbolPattern))
                {
                    query = query.Where(system => system.Symbol.Contains(symbolPattern));
                }

                return query.Take(100).ToList();
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] Search failed: {Error}", e.Message);
                return new List<DatabaseManager.IndexedSystem>();
            }
        }

        public int GetIndexedSystemCount()
        {
            var db = _getConnection();
            if (db == null)
            {
                return 0;
            }

            try
            {
                return db.Table<DatabaseManager.IndexedSystem>().Count();
            }
            catch
            {
                return 0;
            }
        }
    }

    internal sealed class SqliteJumpGateRepository : IJumpGateRepository
    {
        private readonly Func<SQLiteConnection> _getConnection;

        public SqliteJumpGateRepository(Func<SQLiteConnection> getConnection)
        {
            _getConnection = getConnection;
        }

        public void StoreJumpGateWaypoints(IEnumerable<(string waypointSymbol, string systemSymbol)> waypoints)
        {
            var db = _getConnection();
            if (db == null)
            {
                return;
            }

            try
            {
                db.RunInTransaction(() =>
                {
                    foreach (var (waypointSymbol, systemSymbol) in waypoints)
                    {
                        db.Insert(
                            new DatabaseManager.IndexedJumpGate
                            {
                                WaypointSymbol = waypointSymbol,
                                SystemSymbol = systemSymbol,
                                ConnectionsJson = null
                            },
                            "OR IGNORE");
                    }
                });
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] StoreJumpGateWaypoints failed: {Error}", e.Message);
            }
        }

        public void StoreJumpGateConnections(string waypointSymbol, List<string> connections)
        {
            var db = _getConnection();
            if (db == null)
            {
                return;
            }

            try
            {
                string csv = connections != null ? string.Join(",", connections) : string.Empty;
                db.Execute("UPDATE jump_gate_index SET ConnectionsJson = ? WHERE WaypointSymbol = ?", csv, waypointSymbol);
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] StoreJumpGateConnections failed: {Error}", e.Message);
            }
        }

        public List<DatabaseManager.IndexedJumpGate> GetPendingJumpGates()
        {
            var db = _getConnection();
            if (db == null)
            {
                return new List<DatabaseManager.IndexedJumpGate>();
            }

            try
            {
                return db.Table<DatabaseManager.IndexedJumpGate>()
                    .Where(jumpGate => jumpGate.ConnectionsJson == null)
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] GetPendingJumpGates failed: {Error}", e.Message);
                return new List<DatabaseManager.IndexedJumpGate>();
            }
        }

        public List<DatabaseManager.IndexedJumpGate> GetAllJumpGateConnections()
        {
            var db = _getConnection();
            if (db == null)
            {
                return new List<DatabaseManager.IndexedJumpGate>();
            }

            try
            {
                return db.Table<DatabaseManager.IndexedJumpGate>()
                    .Where(jumpGate => jumpGate.ConnectionsJson != null)
                    .ToList();
            }
            catch (Exception e)
            {
                Log.Error("[DatabaseManager] GetAllJumpGateConnections failed: {Error}", e.Message);
                return new List<DatabaseManager.IndexedJumpGate>();
            }
        }

        public int GetIndexedJumpGateCount()
        {
            var db = _getConnection();
            if (db == null)
            {
                return 0;
            }

            try
            {
                return db.Table<DatabaseManager.IndexedJumpGate>().Count();
            }
            catch
            {
                return 0;
            }
        }
    }
}
