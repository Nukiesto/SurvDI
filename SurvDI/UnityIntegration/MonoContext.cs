using System.Collections.Generic;
using SurvDI.Core.Container;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvDI.UnityIntegration
{
    public class MonoContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;
        [SerializeField] private bool bindInstancesOnSceneInRuntime = true;

        private static List<object> GetAllMonobehavsOnScene()
        {
            var monoBehavs = new List<object>();
            var rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();

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
            {
                if (beh is Installer installer)
                {
                    installer.Container = container;
                    installer.Installing();
                }
            }
        }

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
            }
            
            var list = GetAllMonobehavsOnScene();
            
            if (bindInstancesOnSceneInRuntime)
                DiController.InjectInstances(container, list);
        }

        protected override void OnPostInstalling(DiContainer container)
        {
            container.OnBindNewInstanceEvent -= OnBindNewToInitThisContextUnits;
        }
    }
}