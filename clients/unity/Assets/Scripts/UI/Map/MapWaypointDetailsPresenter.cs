using System;
using System.Collections.Generic;
using System.Linq;
using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceTraders.UI.Map
{
    internal static class MapWaypointDetailsPresenter
    {
        public static void ApplyGalaxySystemSelectionDetails(
            Label waypointSymbolLabel,
            Label waypointTypeLabel,
            Label waypointCoordsLabel,
            Label waypointDescriptionLabel,
            Label extraInfoTitleLabel,
            VisualElement extraContentContainer,
            IndexedSystem system,
            Action onOpenSystem)
        {
            if (waypointSymbolLabel != null) waypointSymbolLabel.text = system.Symbol;
            if (waypointTypeLabel != null) waypointTypeLabel.text = system.Type.Replace("_", " ");
            if (waypointCoordsLabel != null) waypointCoordsLabel.text = $"({system.X}, {system.Y})";
            if (waypointDescriptionLabel != null) waypointDescriptionLabel.text = "Use OPEN SYSTEM to view waypoints and details.";
            extraContentContainer?.Clear();
            if (extraInfoTitleLabel != null) extraInfoTitleLabel.text = "System Info";

            if (extraContentContainer == null)
            {
                return;
            }

            extraContentContainer.Add(new Label($"Sector: {system.SectorSymbol}"));
            extraContentContainer.Add(new Label($"Waypoints: {system.WaypointCount}"));
            if (!string.IsNullOrEmpty(system.KnownFacilities))
            {
                extraContentContainer.Add(new Label($"Facilities: {system.KnownFacilities.Replace(",", ", ")}"));
            }

            AddSectionTitle(extraContentContainer, "Actions");

            var openSystemButton = new Button(() => onOpenSystem?.Invoke()) { text = "OPEN SYSTEM" };
            openSystemButton.AddToClassList("button");
            openSystemButton.style.width = 140;
            openSystemButton.style.height = 28;
            openSystemButton.style.marginTop = 4;
            extraContentContainer.Add(openSystemButton);
        }

        public static void ApplySystemWaypointSelectionDetails(
            Label waypointSymbolLabel,
            Label waypointTypeLabel,
            Label waypointCoordsLabel,
            Label waypointDescriptionLabel,
            VisualElement extraContentContainer,
            SystemWaypoint waypoint)
        {
            if (waypointSymbolLabel != null) waypointSymbolLabel.text = waypoint.Symbol;
            if (waypointTypeLabel != null) waypointTypeLabel.text = waypoint.Type.ToString().Replace("_", " ");
            if (waypointCoordsLabel != null) waypointCoordsLabel.text = $"({waypoint.X}, {waypoint.Y})";
            if (waypointDescriptionLabel != null) waypointDescriptionLabel.text = "Loading details...";
            extraContentContainer?.Clear();
        }

        public static void ApplyWaypointSelectionDetails(
            Label waypointSymbolLabel,
            Label waypointTypeLabel,
            Label waypointCoordsLabel,
            Label waypointDescriptionLabel,
            Waypoint waypoint)
        {
            if (waypointSymbolLabel != null) waypointSymbolLabel.text = waypoint.Symbol;
            if (waypointTypeLabel != null) waypointTypeLabel.text = waypoint.Type.ToString().Replace("_", " ");
            if (waypointCoordsLabel != null) waypointCoordsLabel.text = $"({waypoint.X}, {waypoint.Y})";
            if (waypointDescriptionLabel != null) waypointDescriptionLabel.text = WaypointDescriptionBuilder.Build(waypoint);
        }

        public static string BuildWaypointListDetails(SystemWaypoint waypoint, Waypoint detailedWaypoint)
        {
            var typeText = waypoint.Type.ToString().Replace("_", " ");
            var tags = GetWaypointFacilityTags(detailedWaypoint);
            if (tags.Count == 0)
            {
                return typeText;
            }

            return $"{typeText} | {string.Join(" ", tags.Select(tag => $"[{tag}]"))}";
        }

        public static void AddSectionTitle(VisualElement container, string text)
        {
            var label = new Label(text)
            {
                style =
                {
                    marginTop = 8,
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.8f, 0.8f, 0.8f)
                }
            };
            container.Add(label);
        }

        public static string SummarizeTradeGoods(IEnumerable<TradeGood> goods)
        {
            if (goods == null)
            {
                return "-";
            }

            var names = goods
                .Select(good => good?.Symbol.ToString())
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Take(6)
                .ToList();

            if (names.Count == 0)
            {
                return "-";
            }

            return string.Join(", ", names);
        }

        private static List<string> GetWaypointFacilityTags(Waypoint waypoint)
        {
            var tags = new List<string>();
            if (waypoint?.Traits == null)
            {
                return tags;
            }

            if (waypoint.Traits.Any(trait => trait.Symbol == WaypointTraitSymbol.MARKETPLACE)) tags.Add("MARKET");
            if (waypoint.Traits.Any(trait => trait.Symbol == WaypointTraitSymbol.SHIPYARD)) tags.Add("SHIPYARD");
            if (waypoint.Traits.Any(trait => trait.Symbol == WaypointTraitSymbol.UNDERCONSTRUCTION)) tags.Add("CONSTRUCT");
            return tags;
        }
    }
}
