using System;
using System.Collections.Generic;
using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    public class DestroyHandlerContainerUnit : MonoBehaviour
    {
        public event Action OnDestroyEvent;
        private readonly List<ContainerUnit> _registeredUnits = new List<ContainerUnit>();

        public void Register(ContainerUnit unit)
        {
            if (unit == null || _registeredUnits.Contains(unit))
                return;

            _registeredUnits.Add(unit);
            OnDestroyEvent += unit.Dispose;
            unit.OnDisposeEvent += () =>
            {
                _registeredUnits.Remove(unit);
                OnDestroyEvent -= unit.Dispose;
            };
        }

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }
    }
}
