using System.Collections.Generic;
using SurvDI.Core.Container;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvDI.UnityIntegration
{
    [DisallowMultipleComponent]
    public class MonoContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;
        [SerializeField] private bool bindInstancesOnSceneInRuntime = true;

        private static List<MonoBehaviour> GetAllMonobehavsOnScene(Scene scene)
        {
            var monoBehavs = new List<MonoBehaviour>();
            var rootObjs = new List<GameObject>(scene.rootCount);
            scene.GetRootGameObjects(rootObjs);

            foreach (var root in rootObjs)
            {
                if (root.GetComponent<ProjectContext>() != null)
                    continue;
                root.GetComponentsInChildren(true, monoBehavs);
            }

            return monoBehavs;
        }

        private static void InitInstallersOnScene(DiContainer container, List<MonoBehaviour> monoBehavs)
        {
            foreach (var beh in monoBehavs)
                if (beh is Installer installer)
                    installer.InstallingInternal(container);
        }

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
            if (installers != null)
                foreach (var installer in installers)
                    installer.InstallingInternal(container);

            var list = GetAllMonobehavsOnScene(scene);
            InitInstallersOnScene(container, list);
            if (bindInstancesOnSceneInRuntime)
                DiController.InjectInstances(list);
        }

        protected override void OnPostInstalling(DiContainer container, Scene scene)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}
