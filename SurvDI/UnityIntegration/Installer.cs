using Plugins.SurvDI.Core.Container;
using UnityEngine;

namespace Plugins.SurvDI.UnityIntegration
{
    public abstract class Installer : MonoBehaviour
    {
        public DiContainer Container { get; set; }
        
        public abstract void Installing();
    }
}