using VContainer;
using VContainer.Unity;
using SpaceTraders.API;
using SpaceTraders.Core;
using UnityEngine;

namespace SpaceTraders
{
    /// <summary>
    /// This is the Project Root LifetimeScope.
    /// It should be assigned in VContainerSettings (Assets -> Create -> VContainer -> VContainer Settings).
    /// All registrations here are global and persist across scene changes.
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Register Managers/Services as Singletons in the Project Root.
            // These should be attached to the GameLifetimeScope prefab or its children.
            builder.RegisterComponentInHierarchy<DatabaseManager>();
            builder.RegisterComponentInHierarchy<AuthManager>();
            builder.RegisterComponentInHierarchy<SpaceTradersClient>();
            builder.RegisterComponentInHierarchy<APIService>();
            builder.RegisterComponentInHierarchy<UniverseSyncManager>();
            builder.RegisterComponentInHierarchy<GameManager>();
        }
    }
}
