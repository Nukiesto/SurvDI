using System.Collections.Generic;
using SurvDI.Core.Container;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvDI.UnityIntegration
{
    public abstract class MonoContextBase : MonoBehaviour
    {
        private readonly List<ContainerUnit> _thisContextUnits = new();

        private bool _isInstalled;

        protected void OnDestroyInvoke()
        {
            while (_thisContextUnits.Count > 0)
                foreach (var thisContextUnit in _thisContextUnits)
                {
                    thisContextUnit.Dispose();
                    break;
                }
        }

        protected void OnBindNewToInitThisContextUnits(DiContainer container, ContainerUnit unit)
        {
            DiController.InitNewInstance(unit, true);
            AddNewInstanceThisContext(unit);
        }

        public void AddNewInstanceThisContext(ContainerUnit unit)
        {
            if (_thisContextUnits.Contains(unit))
                return;

            _thisContextUnits.Add(unit);
            unit.OnDisposeEvent += () =>
            {
                _thisContextUnits.Remove(unit);
            };
        }
        protected abstract void OnInstalling(DiContainer container, Scene scene);
        protected abstract void OnPreInstalling(DiContainer container, Scene scene);
        protected abstract void OnPostInstalling(DiContainer container, Scene scene);

        public void Installing(DiContainer container, Scene scene)
        {
            if (!_isInstalled)
            {
                _isInstalled = true;
                OnPreInstalling(container, scene);
                OnInstalling(container, scene);
                OnPostInstalling(container, scene);
            }
        }
    }
}
