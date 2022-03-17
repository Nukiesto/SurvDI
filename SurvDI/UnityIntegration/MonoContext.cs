using System.Collections.Generic;
using System.Reflection;
using Plugins.SurvDI.Application.Interfaces;
using Plugins.SurvDI.Core.Container;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugins.SurvDI.UnityIntegration
{
    public class MonoContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;
        [SerializeField] private bool bindInstancesOnSceneInRuntime = true;
        
        private void Awake()
        {
            CheckContexts(this);
        }

        public override void OnInstalling(DiContainer container)
        {
            foreach (var installer in installers)
            {
                installer.Container = container;
                installer.Installing();
            }
            
            var list = GetAllMonobehavsOnScene();
           
            if (bindInstancesOnSceneInRuntime)
                InitInstancesOnScene(container, list);
        }

        private static List<object> GetAllMonobehavsOnScene()
        {
            var monoBehavs = new List<object>();
            var rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var root in rootObjs)
                monoBehavs.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));

            return monoBehavs;
        }

        private static void InitInstancesOnScene(DiContainer container, List<object> monoBehavs)
        {
            foreach (var beh in monoBehavs)
            {
                if (beh == null)
                    continue;
                var type = beh.GetType();
                var attr = (BindAttribute) type.GetCustomAttribute(typeof(BindAttribute));
                if (attr == null)
                    continue;
                if (attr.Multy)
                    container.BindInstanceMulti(type, beh, attr.InjectMode);
                else
                    container.BindInstanceSingle(type, beh, attr.InjectMode);
            }
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
    }
}