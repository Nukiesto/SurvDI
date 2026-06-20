using System.Collections.Generic;
using NUnit.Framework;
using SurvDI.Core.Common;
using SurvDI.Core.Container;

namespace SurvDI.Tests
{
    public class ContainerLifecycleTests
    {
        private interface IPlugin
        {
        }

        private sealed class Plugin : IPlugin
        {
        }

        private sealed class MultiConsumer
        {
            [InjectMulti] public List<IPlugin> Plugins;
        }

        private sealed class ConcreteMultiConsumer
        {
            [InjectMulti] public List<Plugin> Plugins;
        }

        [Test]
        public void DisposeMultiBinding_RemovesItFromResolveMultiIndexes()
        {
            var container = new DiContainer();
            var plugin = new Plugin();

            var unit = container.BindInstanceMulti(plugin);

            CollectionAssert.Contains(container.ResolveMulti<IPlugin>(), plugin);
            CollectionAssert.Contains(container.ResolveMulti<Plugin>(), plugin);

            unit.Dispose();

            CollectionAssert.DoesNotContain(container.ResolveMulti<IPlugin>(), plugin);
            CollectionAssert.DoesNotContain(container.ResolveMulti<Plugin>(), plugin);
        }

        [Test]
        public void NewMultiBinding_IsAddedAndRemovedFromExistingInjectMultiList()
        {
            var container = new DiContainer();
            ContainerInitEvents.InitEvents(container);
            var consumer = new MultiConsumer();
            var plugin = new Plugin();

            container.BindInstanceSingle(consumer);
            var unit = container.BindInstanceMulti(plugin);

            Assert.That(consumer.Plugins, Is.Not.Null);
            CollectionAssert.Contains(consumer.Plugins, plugin);

            unit.Dispose();

            CollectionAssert.DoesNotContain(consumer.Plugins, plugin);
        }

        [Test]
        public void InterfacesAndSelf_RegistersConcreteTypeWithoutDuplicatingResolveMulti()
        {
            var container = new DiContainer();
            var plugin = new Plugin();

            container.BindInstanceMulti(plugin, InjectMode.InterfacesAndSelf);

            var concreteUnits = container.GetInterfaceUnits<Plugin>();
            Assert.AreEqual(1, concreteUnits.Count);
            Assert.AreSame(plugin, concreteUnits[0]);

            var resolved = container.ResolveMulti<Plugin>();
            Assert.AreEqual(1, resolved.Count);
            Assert.AreSame(plugin, resolved[0]);
        }

        [Test]
        public void InjectMultiConcreteList_DoesNotDuplicateSelfAndMultiIndex()
        {
            var container = new DiContainer();
            var consumer = new ConcreteMultiConsumer();
            var plugin = new Plugin();

            container.BindInstanceMulti(plugin, InjectMode.InterfacesAndSelf);
            container.BindInstanceSingle(consumer);

            Assert.DoesNotThrow(container.InvokeInjectAll);
            Assert.AreEqual(1, consumer.Plugins.Count);
            Assert.AreSame(plugin, consumer.Plugins[0]);
        }
    }
}
