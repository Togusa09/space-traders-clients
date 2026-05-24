using SpaceTraders.Generated.Model;

namespace SpaceTraders.UI.Map
{
    internal static class MapModeTransitionResolver
    {
        public static string GetSystemLoadTarget(string selectedSystemSymbol, string selectedSymbol, SpaceTraders.Generated.Model.System currentSystem)
        {
            string target = !string.IsNullOrEmpty(selectedSystemSymbol) ? selectedSystemSymbol : selectedSymbol;
            if (string.IsNullOrEmpty(target)) return null;
            if (!string.IsNullOrEmpty(currentSystem?.Symbol) && currentSystem.Symbol == target) return null;
            return target;
        }
    }
}
