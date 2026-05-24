using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpaceTraders.UI.Map
{
    internal static class MapSelectionMath
    {
        public static T FindClosest<T>(IEnumerable<T> items, Vector2 targetPoint, float threshold, Func<T, Vector2> getWorldPosition)
            where T : class
        {
            if (items == null || getWorldPosition == null) return null;

            return items
                .Select(item => (Item: item, Distance: Vector2.Distance(getWorldPosition(item), targetPoint)))
                .Where(x => x.Distance < threshold)
                .OrderBy(x => x.Distance)
                .Select(x => x.Item)
                .FirstOrDefault();
        }
    }
}
