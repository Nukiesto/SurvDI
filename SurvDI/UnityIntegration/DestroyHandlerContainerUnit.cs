using System;
using UnityEngine;

namespace SurvDI.UnityIntegration.UnityIntegration
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