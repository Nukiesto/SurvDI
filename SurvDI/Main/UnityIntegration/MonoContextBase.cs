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
        protected abstract void OnInstalling(DiContainer container);
        protected abstract void OnPreInstalling(DiContainer container);
        protected abstract void OnPostInstalling(DiContainer container);
       
        public void Installing(DiContainer container)
        {
            if (!_isInstalled)
            {
                _isInstalled = true;
                OnPreInstalling(container);
                OnInstalling(container);
                OnPostInstalling(container);
            }
        }
    }
}