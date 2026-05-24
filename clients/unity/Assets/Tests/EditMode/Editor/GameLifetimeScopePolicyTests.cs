using NUnit.Framework;
using SpaceTraders;

namespace SpaceTraders.Tests.EditMode.Editor
{
    public class GameLifetimeScopePolicyTests
    {
        [Test]
        public void ShouldSkipFallbackScopeCreation_ReturnsTrue_ForRunTestsFlag()
        {
            bool result = GameLifetimeScopePolicy.ShouldSkipFallbackScopeCreation("-batchmode -runTests -testResults results.xml");

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldSkipFallbackScopeCreation_ReturnsTrue_ForTestPlatformFlag()
        {
            bool result = GameLifetimeScopePolicy.ShouldSkipFallbackScopeCreation("-batchmode -testPlatform PlayMode");

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldSkipFallbackScopeCreation_ReturnsFalse_ForNormalEditorCommandLine()
        {
            bool result = GameLifetimeScopePolicy.ShouldSkipFallbackScopeCreation("Unity.exe -projectPath c:/Source/Unity/SpaceTraders-Unity/clients/unity");

            Assert.IsFalse(result);
        }
    }
}
