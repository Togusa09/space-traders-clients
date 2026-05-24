using UnityEngine.UIElements;

namespace SpaceTraders.UI.Map
{
    internal sealed class MapPanelBindings
    {
        public VisualElement SystemList { get; private set; }
        public VisualElement MapContainer { get; private set; }
        public VisualElement WaypointsLayer { get; private set; }
        public TextField SearchField { get; private set; }
        public Label SelectedSystemLabel { get; private set; }
        public VisualElement SystemDetailPanel { get; private set; }
        public VisualElement LegendItems { get; private set; }
        public VisualElement LegendContent { get; private set; }
        public Button ViewGalaxyButton { get; private set; }
        public Button PrevPageButton { get; private set; }
        public Button NextPageButton { get; private set; }
        public Label PageInfoLabel { get; private set; }
        public Button LegendToggleButton { get; private set; }
        public Label WaypointSymbolLabel { get; private set; }
        public Label WaypointTypeLabel { get; private set; }
        public Label WaypointCoordsLabel { get; private set; }
        public Label WaypointDescLabel { get; private set; }
        public Label ExtraInfoTitleLabel { get; private set; }
        public VisualElement ExtraContentContainer { get; private set; }
        public DropdownField TypeFilter { get; private set; }
        public DropdownField FacilityFilter { get; private set; }

        public static bool TryCreate(VisualElement panel, out MapPanelBindings bindings)
        {
            bindings = new MapPanelBindings
            {
                SystemList = panel.Q<VisualElement>("system-list"),
                MapContainer = panel.Q<VisualElement>("map-container"),
                WaypointsLayer = panel.Q<VisualElement>("waypoints-layer"),
                SearchField = panel.Q<TextField>("system-search"),
                SelectedSystemLabel = panel.Q<Label>("selected-system-title"),
                SystemDetailPanel = panel.Q<VisualElement>("waypoint-details"),
                LegendItems = panel.Q<VisualElement>("legend-items"),
                LegendContent = panel.Q<VisualElement>("legend-content"),
                ViewGalaxyButton = panel.Q<Button>("view-galaxy-btn"),
                PrevPageButton = panel.Q<Button>("prev-page"),
                NextPageButton = panel.Q<Button>("next-page"),
                PageInfoLabel = panel.Q<Label>("page-info"),
                LegendToggleButton = panel.Q<Button>("legend-toggle"),
                WaypointSymbolLabel = panel.Q<Label>("wp-symbol"),
                WaypointTypeLabel = panel.Q<Label>("wp-type"),
                WaypointCoordsLabel = panel.Q<Label>("wp-coords"),
                WaypointDescLabel = panel.Q<Label>("wp-desc"),
                ExtraInfoTitleLabel = panel.Q<Label>("extra-info-title"),
                ExtraContentContainer = panel.Q<VisualElement>("extra-content-container"),
                TypeFilter = panel.Q<DropdownField>("type-filter"),
                FacilityFilter = panel.Q<DropdownField>("facility-filter")
            };

            return bindings.MapContainer != null && bindings.SystemList != null;
        }
    }
}
