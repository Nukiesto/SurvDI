using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    public abstract class Installer : MonoBehaviour
    {
        private bool _canInstall = true;
        protected DiContainer Container { get; private set; }

        internal void InstallingInternal(DiContainer container)
        {
            Container = container;
            if (_canInstall)
            {
                _canInstall = false;
                Installing();
            }
        }
        public abstract void Installing();
    }
}