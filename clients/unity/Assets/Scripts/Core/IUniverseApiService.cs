using System.Threading.Tasks;
using SpaceTraders.Generated.Model;

namespace SpaceTraders.Core
{
    internal interface IUniverseApiService
    {
        Task<GetSystems200Response> GetSystems(int page = 1, int limit = 10);
        Task<GetJumpGate200Response> GetJumpGate(string systemSymbol, string waypointSymbol);
    }
}
