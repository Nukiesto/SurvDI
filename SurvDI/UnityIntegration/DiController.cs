using System.Collections.Generic;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Container;
using SurvDI.Core.Services.SavingIntegration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvDI.UnityIntegration
{
    public class DiController : MonoBehaviour
    {
        public static DiController Instance { get; private set; }
        public DiContainer Container { get; private set; }

        private MonoContext _monoContext;
        
        [RuntimeInitializeOnLoadMethod]
        private static void EnterPlayMode()
        {
            new GameObject(nameof(DiController)).AddComponent<DiController>();
        }

        private void Awake()
        {
            Init();
        }
        private void Init()
        {
            Debug.Log("Init");
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnLoadScene;

            Container = new DiContainer();
            Container.BindInstanceSingle(Container);
            Context.InitEvents(Container);
            
            if (!Container.ContainsSingle<SavingModule>())
                Container.BindSingle<SavingModule>();
            
            var (projectContextGo, projectContext) = InstallContext<ProjectContext>();
            var (monoContextGo, monoContext) = InstallContext<MonoContext>();
            _monoContext = monoContext;
            
            if (projectContextGo.GetComponent<Runner>() == null)
                projectContextGo.AddComponent<Runner>().Init(Container);
            
            DontDestroyOnLoad(projectContextGo);
            
            Container.LoadSavingAll();
            Container.InvokeInjectAll();
            Container.InvokeConstructorsAll();
            
            DontDestroyOnLoad(projectContextGo);
            var toPreInits = Container.GetInterfaceUnitsContainers<IPreInit>();
            foreach (var containerUnit in toPreInits)
            {
                if (containerUnit.CanPreInit)
                {
                    containerUnit.CanPreInit = false;
                    if (containerUnit.Object is IPreInit init)
                        init.PreInit();
                }
            }
            var toInits = Container.GetInterfaceUnitsContainers<IInit>();
            foreach (var containerUnit in toInits)
            {
                if (containerUnit.CanInit)
                {
                    containerUnit.CanInit = false;
                    if (containerUnit.Object is IInit init)
                        init.Init();
                }
            }
            var toPostInits = Container.GetInterfaceUnitsContainers<IPostInit>();
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
            Instance = null;
            SceneManager.sceneLoaded -= OnLoadScene;
        }
        private void OnLoadScene(Scene newScene, LoadSceneMode loadSceneMode)
        {
            GetOnScene<MonoContext>().obj.Installing(Container);
        }

        private (GameObject go, T obj) InstallContext<T>() where T : MonoContextBase
        {
            var (go, context) = GetOnScene<T>();
            if (context != null)
                context.Installing(Container);
            return (go, context);
        }
        private static (GameObject go, T obj) GetOnScene<T>() where T : class
        {
            var rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
            var nameT = typeof(T).Name;
            foreach (var root in rootObjs)
            {
                if (root.name == nameT)
                    if (root.TryGetComponent<T>(out var obj))
                        return (root, obj);
            }
            foreach (var root in rootObjs)
            {
                if (root.TryGetComponent<T>(out var obj))
                    return (root, obj);
            }

            return (null, null);
        }

        public static void Inject(GameObject go)
        {
            var monoContext = Instance._monoContext;
            if (monoContext == null)
                Debug.LogWarning("MonoContext is null, Dispose is willn`t work");
            
            var list = go.GetComponents<MonoBehaviour>();
            var container = Instance.Container;

            foreach (var monoBehaviour in list)
            {
                var containerUnit = InitBeh(monoBehaviour, container);
                if (containerUnit == null) continue;
                
                InitNewInstance(containerUnit);
                if (monoContext != null)
                    monoContext.AddNewInstanceThisContext(containerUnit);
            }
        }

        public static void InitNewInstance(ContainerUnit containerUnit)
        {
            var container = Instance.Container;
            
            if (containerUnit.Object is MonoBehaviour monoBeh)
            {
                var go = monoBeh.gameObject;

                var destroyHandler = go.GetComponent<DestroyHandlerContainerUnit>() 
                                     ?? go.AddComponent<DestroyHandlerContainerUnit>();
                
                destroyHandler.OnDestroyEvent += containerUnit.Dispose;
                
                containerUnit.LoadSaveable();
                containerUnit.InvokeInjectsOnInit(container);
                
                if (containerUnit.CanPreInit)
                {
                    containerUnit.CanPreInit = false;
                    if (containerUnit.Object is IPreInit init)
                        init.PreInit();
                }
                if (containerUnit.CanInit)
                {
                    containerUnit.CanInit = false;
                    if (containerUnit.Object is IInit init)
                        init.Init();
                }
                if (containerUnit.CanPostInit)
                {
                    containerUnit.CanPostInit = false;
                    if (containerUnit.Object is IPostInit init)
                        init.PostInit();
                }
            }
        }
        public static ContainerUnit InitBeh(object beh, DiContainer container)
        {
            var type = beh.GetType();
            var attr = (BindAttribute) type.GetCustomAttribute(typeof(BindAttribute));
            if (attr == null)
                return null;
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (attr.Multy)
                return container.BindInstanceMulti(type, beh, attr.InjectMode);
            return container.BindInstanceSingle(type, beh, attr.InjectMode);
        }
        public static void InjectInstances(DiContainer container, List<object> monoBehavs)
        {
            foreach (var beh in monoBehavs)
            {
                if (beh == null)
                    continue;
                InitBeh(beh, container);
            }
        }
    }
}