using VContainer;
using VContainer.Unity;
using SpaceTraders.API;
using SpaceTraders.Core;
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
        }
    }
}
