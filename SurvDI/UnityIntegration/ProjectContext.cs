using System.Linq;
using SurvDI.Core.Container;
using UnityEngine;

namespace SurvDI.UnityIntegration
{
    public class ProjectContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;

        private void OnDestroy()
        {
            OnDestroyInvoke();
        }

        protected override void OnPreInstalling(DiContainer container)
        {
            container.OnBindNewInstanceEvent += OnBindNewToInitThisContextUnits;
        }

        protected override void OnInstalling(DiContainer container)
        {
            if (installers != null)
            {
                foreach (var installer in installers)
                {
                    installer.Container = container;
                    installer.Installing();
                }

                var list = GetComponents<MonoBehaviour>().Cast<object>().ToList();
                DiController.InjectInstances(container, list);
            }
        }

        protected override void OnPostInstalling(DiContainer container)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}