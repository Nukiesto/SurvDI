using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration.UnityIntegration
{
    public abstract class Installer : MonoBehaviour
    {
        public DiContainer Container { get; set; }
        
        public abstract void Installing();
    }
}