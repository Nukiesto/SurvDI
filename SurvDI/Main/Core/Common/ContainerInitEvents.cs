using System;
using System.Collections.Generic;
using SurvDI.Core.Container;

namespace SurvDI.Core.Common
{
    public static class ContainerInitEvents
    {
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
}