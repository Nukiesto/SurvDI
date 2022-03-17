using System;
using UnityEngine;

namespace Plugins.SurvDI.UnityIntegration
{
    public class DestroyHandlerContainerUnit : MonoBehaviour
    {
        public event Action OnDestroyEvent;

        private void OnDestroy()
        {
            OnDestroyEvent?.Invoke();
        }
    }
}