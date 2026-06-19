using SurvDI.Application.Interfaces;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    [Bind(Multy = true)]
    public class MonoBehaviourBind : MonoBehaviour
    {
        private bool _isInjected;

        private void Awake()
        {
            TryInject();
        }

        private void Start()
        {
            TryInject();
        }

        private void TryInject()
        {
            if (_isInjected)
                return;
            if (!DiController.CanInject)
                return;

            DiController.InjectGameObject(gameObject);
            _isInjected = true;
        }
    }
}
