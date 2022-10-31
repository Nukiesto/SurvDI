using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    public abstract class Installer : MonoBehaviour
    {
        protected DiContainer Container { get; private set; }

        internal void InstallingInternal(DiContainer container)
        {
            Container = container;
            Installing();
        }
        public abstract void Installing();
    }
}