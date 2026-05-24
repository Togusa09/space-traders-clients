using SpaceTraders.Core;
using SpaceTraders.Generated.Model;
using UnityEngine;

namespace SpaceTraders.UI.Map
{
    internal static class MapStyleResolver
    {
        public static MapPresenter.IconStyle GetSystemStyle(IndexedSystem system)
        {
            var style = new MapPresenter.IconStyle
            {
                Radius = 3f,
                StrokeWidth = 0,
                FillColor = Color.white,
                Shape = MapPresenter.IconShape.Circle
            };

            string type = (system.Type ?? string.Empty).Replace("_", string.Empty);
            if (type == "REDSTAR") style.FillColor = new Color(1f, 0.4f, 0.4f);
            else if (type == "BLUESTAR") style.FillColor = new Color(0.4f, 0.4f, 1f);
            else if (type == "YOUNGSTAR") style.FillColor = new Color(0.6f, 1f, 1f);
            else if (type == "WHITEDWARF") { style.FillColor = Color.white; style.Radius = 2f; }
            else if (type == "BLACKHOLE") { style.FillColor = Color.black; style.StrokeColor = Color.purple; style.StrokeWidth = 1f; }
            else if (type == "NEBULA") { style.FillColor = new Color(1f, 0.2f, 1f, 0.4f); style.Shape = MapPresenter.IconShape.Square; style.Radius = 5f; }

            return style;
        }

        public static MapPresenter.IconStyle GetWaypointStyle(SystemWaypoint waypoint)
        {
            var style = new MapPresenter.IconStyle
            {
                Radius = 4f,
                StrokeWidth = 0,
                FillColor = Color.white,
                Shape = MapPresenter.IconShape.Circle
            };

            switch (waypoint.Type)
            {
                case WaypointType.PLANET:
                    style.FillColor = new Color(0.2f, 0.6f, 1f);
                    break;
                case WaypointType.MOON:
                    style.FillColor = Color.gray;
                    style.Radius = 2f;
                    break;
                case WaypointType.ORBITALSTATION:
                    style.FillColor = Color.yellow;
                    style.Shape = MapPresenter.IconShape.Square;
                    style.Radius = 3f;
                    break;
                case WaypointType.JUMPGATE:
                    style.FillColor = new Color(0.7f, 0f, 1f);
                    style.Shape = MapPresenter.IconShape.Diamond;
                    break;
                case WaypointType.ASTEROIDFIELD:
                    style.FillColor = new Color(0.5f, 0.4f, 0.3f);
                    style.Shape = MapPresenter.IconShape.Hexagon;
                    break;
            }

            return style;
        }
    }
}
