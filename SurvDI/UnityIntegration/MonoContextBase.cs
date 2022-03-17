using System;
using System.Collections.Generic;
using System.Linq;
using SurvDI.Core.Common;
using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration.UnityIntegration
{
    public abstract class MonoContextBase : MonoBehaviour, IContext
    {
#if UNITY_2019_4
        private readonly List<ContainerUnit> _thisContextUnits = new List<ContainerUnit>();
#else
        private readonly List<ContainerUnit> _thisContextUnits = new ();
#endif
        protected DiContainer Container { get; set; }

        protected void CheckContexts(MonoContext monoContext)
        {
            var projectContextOnScene = FindObjectsOfType<ProjectContext>();
            if (projectContextOnScene.Length > 1 && !ProjectContext.IsInstantiated)
                throw new Exception("Project Context count dont > 0");

            ProjectContext projectContext = null;
            
            if (projectContextOnScene.Length > 0)
                projectContext = projectContextOnScene.First();
            ProjectContext.Init(projectContext != null ? projectContext.gameObject : null, monoContext);
            
            Container = ProjectContext.Instance.Container;
        }

        protected void OnDestroyInvoke()
        {
            while (_thisContextUnits.Count > 0)
            {
                foreach (var thisContextUnit in _thisContextUnits)
                {
                    Debug.Log("DISPOSE");
                    thisContextUnit.Dispose();
                    break;
                }
            }
        }

        protected void OnBindNewToInitThisContextUnits(DiContainer container, ContainerUnit s)
        {
            _thisContextUnits.Add(s);
            if (s.Object is MonoBehaviour monoBeh)
            {
                var go = monoBeh.gameObject;
                var destroyHandler = go.AddComponent<DestroyHandlerContainerUnit>();
                destroyHandler.OnDestroyEvent += s.Dispose;
            }
            s.OnDisposeEvent += () =>
            {
                if (_thisContextUnits.Contains(s))
                    _thisContextUnits.Remove(s);
            };
        }
        
        public abstract void OnInstalling(DiContainer container);
        public void OnPreInstalling(DiContainer container)
        {
            container.OnBindNewInstanceEvent += OnBindNewToInitThisContextUnits;
        }

        public void OnPostInstalling(DiContainer container)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}