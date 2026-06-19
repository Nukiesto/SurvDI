using System.Linq;
using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    [DisallowMultipleComponent]
    public class ProjectContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;

        private void OnDestroy()
        {
            OnDestroyInvoke();
        }

        protected override void OnPreInstalling(DiContainer container, int sceneId)
        {
            container.OnBindNewInstanceEvent += OnBindNewToInitThisContextUnits;
        }

        protected override void OnInstalling(DiContainer container, int sceneId)
        {
            if (installers != null)
            {
                foreach (var installer in installers)
                    installer.InstallingInternal(container);

                var list = GetComponents<MonoBehaviour>().Cast<object>().ToList();
                DiController.InjectInstances(list);
            }
        }

        protected override void OnPostInstalling(DiContainer container, int sceneId)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}