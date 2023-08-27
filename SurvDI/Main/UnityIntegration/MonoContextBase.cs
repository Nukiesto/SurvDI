using System.Collections.Generic;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Container;
using UnityEngine;

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
            _thisContextUnits.Add(unit);
            unit.OnDisposeEvent += () =>
            {
                if (_thisContextUnits.Contains(unit))
                    _thisContextUnits.Remove(unit);
            };
        }
        protected abstract void OnInstalling(DiContainer container, int sceneId);
        protected abstract void OnPreInstalling(DiContainer container, int sceneId);
        protected abstract void OnPostInstalling(DiContainer container, int sceneId);
       
        public void Installing(DiContainer container, int sceneId)
        {
            if (!_isInstalled)
            {
                _isInstalled = true;
                OnPreInstalling(container, sceneId);
                OnInstalling(container, sceneId);
                OnPostInstalling(container, sceneId);
            }
        }
    }
}