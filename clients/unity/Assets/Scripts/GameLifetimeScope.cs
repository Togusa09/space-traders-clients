using System;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureProjectRootScope()
        {
            if (GameLifetimeScopePolicy.ShouldSkipFallbackScopeCreation(Environment.CommandLine))
            {
                return;
            }

            if (FindAnyObjectByType<GameLifetimeScope>() != null)
            {
                return;
            }

            var root = new GameObject(nameof(GameLifetimeScope));
            root.AddComponent<GameLifetimeScope>();
            DontDestroyOnLoad(root);
            Debug.LogWarning("[GameLifetimeScope] Root scope was not present at startup. Created runtime fallback root.");
        }

        protected override void Configure(IContainerBuilder builder)
        {
            // Register Managers/Services as Singletons in the Project Root.
            // Prefer prefab-wired components, but create deterministic fallbacks when missing.
            var databaseManager = EnsureChildComponent<DatabaseManager>();
            var authManager = EnsureChildComponent<AuthManager>();
            var spaceTradersClient = EnsureChildComponent<SpaceTradersClient>();
            var apiService = EnsureChildComponent<APIService>();
            var universeSyncManager = EnsureChildComponent<UniverseSyncManager>();
            var gameManager = EnsureChildComponent<GameManager>();

            builder.RegisterComponent(databaseManager);
            builder.Register<IApiCacheRepository>(resolver => resolver.Resolve<DatabaseManager>(), Lifetime.Singleton);
            builder.Register<ISystemIndexRepository>(resolver => resolver.Resolve<DatabaseManager>(), Lifetime.Singleton);
            builder.Register<IJumpGateRepository>(resolver => resolver.Resolve<DatabaseManager>(), Lifetime.Singleton);
            builder.RegisterComponent(authManager);
            builder.RegisterComponent(spaceTradersClient);
            builder.RegisterComponent(apiService);
            builder.Register<IUniverseApiService>(resolver => resolver.Resolve<APIService>(), Lifetime.Singleton);
            builder.RegisterComponent(universeSyncManager);
            builder.RegisterComponent(gameManager);
        }

        private T EnsureChildComponent<T>() where T : Component
        {
            var component = GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }

            var child = new GameObject(typeof(T).Name);
            child.transform.SetParent(transform, false);
            component = child.AddComponent<T>();
            Debug.LogWarning($"[GameLifetimeScope] Missing {typeof(T).Name} in hierarchy. Created fallback instance.");
            return component;
        }
    }

    internal static class GameLifetimeScopePolicy
    {
        public static bool ShouldSkipFallbackScopeCreation(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return false;
            }

            // Unity CLI test runs include one or both of these switches.
            return commandLine.IndexOf("-runTests", StringComparison.OrdinalIgnoreCase) >= 0
                || commandLine.IndexOf("-testPlatform", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
