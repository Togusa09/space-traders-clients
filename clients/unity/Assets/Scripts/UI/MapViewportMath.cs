using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceTraders.UI
{
    internal static class MapViewportMath
    {
        public static Vector2 WorldToScreen(Vector2 worldPoint, float zoom, Vector2 offset)
        {
            return worldPoint * zoom + offset;
        }

        public static Vector2 ScreenToWorld(Vector2 localPoint, float zoom, Vector2 offset)
        {
            return (localPoint - offset) / Mathf.Max(zoom, 0.000001f);
        }

        public static Vector2 CenterOnWorldPoint(Rect rect, Vector2 worldPoint, float zoom)
        {
            return (rect.size / 2f) - (worldPoint * zoom);
        }

        public static (Vector2 Offset, float Zoom) FitBounds(IEnumerable<Vector2> points, Rect rect, float minZoom, float maxZoom)
        {
            var list = points.ToList();
            if (list.Count == 0)
            {
                return (rect.size / 2f, 1.0f);
            }

            float minX = list.Min(p => p.x);
            float maxX = list.Max(p => p.x);
            float minY = list.Min(p => p.y);
            float maxY = list.Max(p => p.y);

            float width = Mathf.Max(1f, maxX - minX);
            float height = Mathf.Max(1f, maxY - minY);
            float zoomX = (rect.width * 0.85f) / width;
            float zoomY = (rect.height * 0.85f) / height;
            float zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), minZoom, maxZoom);

            var center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
            var offset = (rect.size / 2f) - (center * zoom);
            return (offset, zoom);
        }
    }
}
