using UnityEngine;
using UnityEngine.UIElements;

namespace SpaceTraders.UI.Map
{
    internal interface IMapInteractionHost
    {
        float MapZoom { get; set; }
        Vector2 MapOffset { get; set; }
        float MinZoom { get; }
        float MaxZoom { get; }
        void HandleMapClickFromInteraction(Vector2 localPosition);
        void RefreshMapFromInteraction();
    }

    internal static class MapInteractionMath
    {
        public static Vector2 ApplyPan(Vector2 currentOffset, Vector2 previousPointer, Vector2 currentPointer)
        {
            return currentOffset + (currentPointer - previousPointer);
        }

        public static (float Zoom, Vector2 Offset) ApplyZoom(
            float currentZoom,
            Vector2 currentOffset,
            Vector2 localMousePosition,
            float wheelDeltaY,
            float minZoom,
            float maxZoom)
        {
            float delta = -wheelDeltaY * 0.1f;
            float safeCurrentZoom = Mathf.Max(currentZoom, 0.000001f);
            float nextZoom = Mathf.Clamp(currentZoom * (1f + delta), minZoom, maxZoom);
            Vector2 nextOffset = localMousePosition - ((localMousePosition - currentOffset) / safeCurrentZoom * nextZoom);
            return (nextZoom, nextOffset);
        }
    }

    internal sealed class MapInteractionManipulator : Manipulator
    {
        private readonly IMapInteractionHost _host;
        private bool _isPanning;
        private Vector2 _lastPointerPosition;

        public MapInteractionManipulator(IMapInteractionHost host)
        {
            _host = host;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<WheelEvent>(OnWheel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<WheelEvent>(OnWheel);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 0)
            {
                _host.HandleMapClickFromInteraction(evt.localPosition);
                evt.StopPropagation();
                return;
            }

            if (evt.button == 1 || evt.button == 2)
            {
                _isPanning = true;
                _lastPointerPosition = evt.localPosition;
                target.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isPanning)
            {
                return;
            }

            _host.MapOffset = MapInteractionMath.ApplyPan(_host.MapOffset, _lastPointerPosition, evt.localPosition);
            _lastPointerPosition = evt.localPosition;
            _host.RefreshMapFromInteraction();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isPanning || (evt.button != 1 && evt.button != 2))
            {
                return;
            }

            _isPanning = false;
            target.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnWheel(WheelEvent evt)
        {
            var next = MapInteractionMath.ApplyZoom(
                _host.MapZoom,
                _host.MapOffset,
                evt.localMousePosition,
                evt.delta.y,
                _host.MinZoom,
                _host.MaxZoom);

            _host.MapZoom = next.Zoom;
            _host.MapOffset = next.Offset;
            _host.RefreshMapFromInteraction();
            evt.StopPropagation();
        }
    }
}
