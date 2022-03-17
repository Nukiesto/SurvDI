using System;
using System.Collections.Generic;
using Plugins.SurvDI.Core.Container;

namespace Plugins.SurvDI.Core.Common
{
    public class Context
    {
        public static void Installing(DiContainer container, IContext context)
        {
            context.OnPreInstalling(container);
            context.OnInstalling(container);
            context.OnPostInstalling(container);
        }

        public static void InitEvents(DiContainer container)
        {
            container.OnBindNewInstanceEvent += OnBindForMultyNeed;
            container.OnRemoveInstanceEvent  += OnRemoveDispose;
        }

        private static void OnBindForMultyNeed(DiContainer container, ContainerUnit s)
        {
            //s.InvokeInjectsOnInit(container);
            //s.InvokeConstructorInit(container);

            var types = new List<Type>();
            types.AddRange(s.Interfaces);
            types.Add(s.BaseType);
            types.Add(s.Type);
                
            foreach (var type in types)
            {
                if (type == null)
                    continue;
                if (!container.ContainersMultyNeed.ContainsKey(type))
                    continue;
                    
                var listNeed = container.ContainersMultyNeed[type];
                foreach (var containerUnit in listNeed)
                    containerUnit.AddNewMulty(type, s);
            }
        }
        private static void OnRemoveDispose(DiContainer container, ContainerUnit s)
        {
            if (s.Object is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public interface IContext
    {
#if UNITY_2019_4
        void OnInstalling(DiContainer container);
        void OnPreInstalling(DiContainer container);
        void OnPostInstalling(DiContainer container);
#else
        public void OnInstalling(DiContainer container);
        public void OnPreInstalling(DiContainer container);
        public void OnPostInstalling(DiContainer container);
#endif
    }
}