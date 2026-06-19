using System.Collections.Generic;
using SurvDI.Core.Container;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvDI.UnityIntegration
{
    [DisallowMultipleComponent]
    public class ProjectContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;
        private readonly List<MonoBehaviour> _monoBehaviours = new();

        private void OnDestroy()
        {
            OnDestroyInvoke();
        }

        protected override void OnPreInstalling(DiContainer container, Scene scene)
        {
            container.OnBindNewInstanceEvent += OnBindNewToInitThisContextUnits;
        }

        protected override void OnInstalling(DiContainer container, Scene scene)
        {
            if (installers == null)
                return;

            foreach (var installer in installers)
                installer.InstallingInternal(container);

            _monoBehaviours.Clear();
            GetComponents(_monoBehaviours);
            DiController.InjectInstances(_monoBehaviours);
        }

        protected override void OnPostInstalling(DiContainer container, Scene scene)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}
