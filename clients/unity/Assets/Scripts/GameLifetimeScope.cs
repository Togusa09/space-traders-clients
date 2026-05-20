using VContainer;
using VContainer.Unity;
using SpaceTraders.API;
using SpaceTraders.Core;
using SpaceTraders.UI;
using UnityEngine;

namespace SpaceTraders
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Register Managers/Services as Singletons in the Root Scope
            builder.RegisterComponentInHierarchy<DatabaseManager>();
            builder.RegisterComponentInHierarchy<AuthManager>();
            builder.RegisterComponentInHierarchy<SpaceTradersClient>();
            builder.RegisterComponentInHierarchy<APIService>();
            builder.RegisterComponentInHierarchy<UniverseSyncManager>();
            builder.RegisterComponentInHierarchy<GameManager>();

            // Register UI Controllers/Presenters to enable Injection
            builder.RegisterComponentInHierarchy<MenuManager>();
            builder.RegisterComponentInHierarchy<RegistrationUI>();
            builder.RegisterComponentInHierarchy<SettingsUI>();
            builder.RegisterComponentInHierarchy<DashboardController>();
            builder.RegisterComponentInHierarchy<MapPresenter>();
            builder.RegisterComponentInHierarchy<FleetPresenter>();
            builder.RegisterComponentInHierarchy<ContractPresenter>();
        }
    }
}
