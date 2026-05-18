using System;

namespace SpaceTraders.API.Models
{
    [Serializable]
    public class Agent
    {
        public string accountId;
        public string symbol;
        public string headquarters;
        public long credits;
        public string startingFaction;
    }

    [Serializable]
    public class RegistrationData
    {
        public string faction;
        public string symbol;
        public string email;
    }

    [Serializable]
    public class RegistrationResult
    {
        public string token;
        public Agent agent;
        public Faction faction;
    }

    [Serializable]
    public class RegistrationResponse
    {
        public RegistrationResult data;
    }

    [Serializable]
    public class AgentResponse
    {
        public Agent data;
    }

    [Serializable]
    public class Meta
    {
        public int total;
        public int page;
        public int limit;
    }

    [Serializable]
    public class FactionTrait
    {
        public string symbol;
        public string name;
        public string description;
    }

    [Serializable]
    public class Faction
    {
        public string symbol;
        public string name;
        public string description;
        public string headquarters;
        public FactionTrait[] traits;
        public bool isRecruiting;
    }

    [Serializable]
    public class FactionsResponse
    {
        public Faction[] data;
        public Meta meta;
    }

    [Serializable]
    public class Contract
    {
        public string id;
        public string factionSymbol;
        public string type;
        public ContractTerms terms;
        public bool accepted;
        public bool fulfilled;
        public string expiration;
        public string deadlineToAccept;
    }

    [Serializable]
    public class ContractTerms
    {
        public string deadline;
        public ContractPayment payment;
        public ContractDeliverable[] deliver;
    }

    [Serializable]
    public class ContractPayment
    {
        public long onAccepted;
        public long onFulfilled;
    }

    [Serializable]
    public class ContractDeliverable
    {
        public string tradeSymbol;
        public string destinationSymbol;
        public int unitsRequired;
        public int unitsFulfilled;
    }

    [Serializable]
    public class ContractsResponse
    {
        public Contract[] data;
        public Meta meta;
    }

    [Serializable]
    public class Ship
    {
        public string symbol;
        public string role;
        public ShipRegistration registration;
        public ShipNav nav;
        public ShipFrame frame;
        public ShipCargo cargo;
        public ShipFuel fuel;
    }

    [Serializable]
    public class ShipRegistration
    {
        public string name;
        public string factionSymbol;
        public string role;
    }

    [Serializable]
    public class ShipNav
    {
        public string systemSymbol;
        public string waypointSymbol;
        public string route;
        public string status;
        public string flightMode;
    }

    [Serializable]
    public class ShipFrame
    {
        public string symbol;
        public string name;
        public string description;
        public int moduleSlots;
        public int mountingPoints;
        public int fuelCapacity;
        public ShipCondition condition;
    }

    [Serializable]
    public class ShipCondition
    {
        public int integrity;
        public int durability;
    }

    [Serializable]
    public class ShipCargo
    {
        public int capacity;
        public int units;
        public ShipCargoItem[] inventory;
    }

    [Serializable]
    public class ShipCargoItem
    {
        public string symbol;
        public string name;
        public string description;
        public int units;
    }

    [Serializable]
    public class ShipFuel
    {
        public int current;
        public int capacity;
        public ShipFuelConsumed consumed;
    }

    [Serializable]
    public class ShipFuelConsumed
    {
        public int amount;
        public string timestamp;
    }

    [Serializable]
    public class ShipsResponse
    {
        public Ship[] data;
        public Meta meta;
    }

    [Serializable]
    public class SystemData
    {
        public string symbol;
        public string sectorSymbol;
        public string type;
        public int x;
        public int y;
        public SystemWaypoint[] waypoints;
        public SystemFaction[] factions;
    }

    [Serializable]
    public class SystemWaypoint
    {
        public string symbol;
        public string type;
        public int x;
        public int y;
    }

    [Serializable]
    public class SystemFaction
    {
        public string symbol;
    }

    [Serializable]
    public class SystemsResponse
    {
        public SystemData[] data;
        public Meta meta;
    }
}
