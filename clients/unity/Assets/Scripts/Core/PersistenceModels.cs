using SQLite;

namespace SpaceTraders.Core
{
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

        // Comma-separated list of connected waypoint symbols, or null if not yet fetched.
        public string ConnectionsJson { get; set; }
    }
}
