using SurvDI.Application.Interfaces;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    [Bind(Multy = true)]
    public class MonoBehaviourBind : MonoBehaviour
    {
        private void Awake()
        {
            DiController.InjectGameObject(gameObject);
        }
    }
}