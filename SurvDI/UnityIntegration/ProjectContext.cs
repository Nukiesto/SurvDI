using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Container;
using SurvDI.Core.Services.SavingIntegration;
using UnityEngine;

namespace SurvDI.UnityIntegration.UnityIntegration
{
    public class ProjectContext : MonoContextBase
    {
        [SerializeField] private Installer[] installers;
        
        public static bool IsInstantiated { get; private set; }
        public static ProjectContext Instance { get; private set; }
        private bool _isInstalled;
        private bool _isEventsInited;
        
        public void Awake()
        {
            if (!IsInstantiated)
                Init(gameObject);
            if (Instance != this && IsInstantiated)
                Destroy(gameObject);
        }

        public static void Init(GameObject gameObjectSet = null, MonoContext monoContext = null)
        {
            IsInstantiated = true;
            
            var projectContextGo = gameObjectSet;
            if (projectContextGo == null)
                projectContextGo = new GameObject("ProjectContext");

            var projectContext = projectContextGo.GetComponent<ProjectContext>();
            if (projectContext == null)
                projectContext = projectContextGo.AddComponent<ProjectContext>();
            
            Instance = projectContext;

            var container = projectContext.Container;
            if (container == null)
            {
                container = new DiContainer();
                projectContext.Container = container;
                container.BindInstanceSingle(container);
            }
            Instance.Container = container;
            Context.Installing(container, projectContext);

            if (!projectContext._isEventsInited)
            {
                projectContext._isEventsInited = true;
                Context.InitEvents(container);
            }

            if (monoContext != null)
                Context.Installing(container, monoContext);
            if (!container.ContainsSingle<SavingModule>())
                container.BindSingle<SavingModule>();

            container.LoadSavingAll();
            container.InvokeInjectAll();
            container.InvokeConstructorsAll();

            if (projectContextGo.GetComponent<Runner>() == null)
            {
                var runner = projectContextGo.AddComponent<Runner>();
                runner.Init(container);
            }
            
            DontDestroyOnLoad(projectContextGo);
            var toPreInits = container.GetInterfaceUnitsContainers<IPreInit>();
            foreach (var containerUnit in toPreInits)
            {
                if (containerUnit.CanPreInit)
                {
                    containerUnit.CanPreInit = false;
                    if (containerUnit.Object is IPreInit init)
                        init.PreInit();
                }
            }
            var toInits = container.GetInterfaceUnitsContainers<IInit>();
            foreach (var containerUnit in toInits)
            {
                if (containerUnit.CanInit)
                {
                    containerUnit.CanInit = false;
                    if (containerUnit.Object is IInit init)
                        init.Init();
                }
            }
            var toPostInits = container.GetInterfaceUnitsContainers<IPostInit>();
            foreach (var containerUnit in toPostInits)
            {
                if (containerUnit.CanPostInit)
                {
                    containerUnit.CanPostInit = false;
                    if (containerUnit.Object is IPostInit init)
                        init.PostInit();
                }
            }
        }
        private void OnDestroy()
        {
            if (Instance == this)
            {
                OnDestroyInvoke();
            
                IsInstantiated = false;
                Container = null;
            }
        }

        public override void OnInstalling(DiContainer container)
        {
            if (!_isInstalled)
            {
                if (installers != null)
                {
                    foreach (var installer in installers)
                    {
                        installer.Container = container;
                        installer.Installing();
                    }
                }

                _isInstalled = true;
            }
        }
    }
}