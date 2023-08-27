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

        private static List<object> GetAllMonobehavsOnScene(int sceneId)
        {
            var monoBehavs = new List<object>();
            var rootObjs = SceneManager.GetSceneByBuildIndex(sceneId).GetRootGameObjects();

            foreach (var root in rootObjs)
            {
                if (root.GetComponent<ProjectContext>() != null)
                    continue;
                monoBehavs.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));
            }
            
            return monoBehavs;
        }
        
        private static void InitInstallersOnScene(DiContainer container, List<object> monoBehavs)
        {
            foreach (var beh in monoBehavs)
                if (beh is Installer installer)
                    installer.InstallingInternal(container);
        }

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
                foreach (var installer in installers)
                    installer.InstallingInternal(container);

            var list = GetAllMonobehavsOnScene(sceneId);
            InitInstallersOnScene(container, list);
            if (bindInstancesOnSceneInRuntime)
                DiController.InjectInstances(list);
        }

        protected override void OnPostInstalling(DiContainer container, int sceneId)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}