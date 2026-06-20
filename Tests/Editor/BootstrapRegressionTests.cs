using System;
using NUnit.Framework;
using SurvDI.Core.Common;
using SurvDI.Core.Container;

namespace SurvDI.Tests
{
    public class BootstrapRegressionTests
    {
        private sealed class MessageManager
        {
        }

        private sealed class WorldManager
        {
            [Inject] public MessageManager MessageManager;
        }

        private sealed class LobbyManager
        {
            [Inject(CanBeNull = true)] public MessageManager MessageManager;
        }

        private sealed class PlayerLoginManager
        {
        }

        private sealed class PlayerSaveCloudManager : IDisposable
        {
            [Inject(CanBeNull = true)] public PlayerLoginManager PlayerLoginManager;
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
                if (PlayerLoginManager != null)
                    PlayerLoginManager.ToString();
            }
        }

        [Test]
        public void GlobalServiceRegisteredBeforeSceneUi_ResolvesWhenInjectRunsAfterSceneBind()
        {
            var container = new DiContainer();
            var worldManager = new WorldManager();
            var messageManager = new MessageManager();

            container.BindInstanceSingle(worldManager);
            container.BindInstanceSingle(messageManager);

            Assert.DoesNotThrow(container.InvokeInjectAll);
            Assert.AreSame(messageManager, worldManager.MessageManager);
        }

        [Test]
        public void MissingRequiredSceneUi_ThrowsUsefulResolveError()
        {
            var container = new DiContainer();
            var worldManager = new WorldManager();

            container.BindInstanceSingle(worldManager);

            var exception = Assert.Throws<Exception>(container.InvokeInjectAll);

            StringAssert.Contains(nameof(MessageManager), exception.Message);
            StringAssert.Contains(nameof(WorldManager), exception.Message);
            Assert.IsNull(worldManager.MessageManager);
        }

        [Test]
        public void OptionalSceneUiDependency_DoesNotBlockBootstrap()
        {
            var container = new DiContainer();
            var lobbyManager = new LobbyManager();

            container.BindInstanceSingle(lobbyManager);

            Assert.DoesNotThrow(container.InvokeInjectAll);
            Assert.IsNull(lobbyManager.MessageManager);
        }

        [Test]
        public void PartiallyInitializedOptionalService_DisposesWithoutThrowing()
        {
            var container = new DiContainer();
            ContainerInitEvents.InitEvents(container);

            var saveManager = new PlayerSaveCloudManager();
            var unit = container.BindInstanceSingle(saveManager);

            Assert.DoesNotThrow(container.InvokeInjectAll);
            Assert.DoesNotThrow(unit.Dispose);
            Assert.IsTrue(saveManager.Disposed);
        }
    }
}
