using NUnit.Framework;
using SurvDI.Core.Services.EventControllerIntegration;

namespace SurvDI.Tests
{
    public class EventModuleTests
    {
        private struct TestSignal
        {
        }

        [Test]
        public void DisposeBeforeInit_DoesNotThrow()
        {
            var module = new EventModule();

            Assert.DoesNotThrow(module.Dispose);
        }

        [Test]
        public void Dispose_UnsubscribesModuleCallbacks()
        {
            var manager = new EventModuleManager();
            var module = new EventModule();
            var received = 0;

            module.Init(manager);
            module.Subscribe<TestSignal>(_ => received++);

            manager.Publish(new TestSignal());
            Assert.AreEqual(1, received);

            module.Dispose();
            manager.Publish(new TestSignal());

            Assert.AreEqual(1, received);
            Assert.DoesNotThrow(module.Dispose);
        }
    }
}
