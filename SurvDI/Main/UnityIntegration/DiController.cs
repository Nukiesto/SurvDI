using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Container;
using SurvDI.Core.Services.SavingIntegration;
using SurvDI.UnityIntegration.Debugging;
using SurvDI.UnityIntegration.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SurvDI.UnityIntegration
{
    public class DiController : MonoBehaviour
    {
        public static DiController Instance { get; private set; }
        public DiContainer Container { get; private set; }

        //Contexts
        private MonoContext _currentSceneMonoContext;
        private bool _isHasProjectContext;

        internal SurvDISettings SurvDISettings { get; private set; }
        private const string SurvDIName = "SurvDISettings";
        private const string SurvDISettingsPath = "Assets/Resources/" + SurvDIName + ".asset";
        private static string ResourcesPath => UnityEngine.Application.dataPath + "/Resources";
        
        [RuntimeInitializeOnLoadMethod]
        private static void EnterPlayMode()
        {
            new GameObject(nameof(DiController)).AddComponent<DiController>();
        }

        private void Awake()
        {
            Init();
        }
        private void OnDestroy()
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnLoadScene;
        }
        private void Init()
        {
            InitSettings();
            
            Debugger.Log("Init DI Controller");
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnLoadScene;

            Container = new DiContainer();
            Container.BindInstanceSingle(Container);
            ContainerInitEvents.InitEvents(Container);
            
            if (!Container.ContainsSingle<SavingModule>())
                Container.BindSingle<SavingModule>();

            ContextInit();
        }

        private void InitSettings()
        {
            if (!Directory.Exists(ResourcesPath))
                Directory.CreateDirectory(ResourcesPath);
            SurvDISettings = Resources.Load<SurvDISettings>(SurvDIName);
            
            if (SurvDISettings == null)
            {
                SurvDISettings = ScriptableObject.CreateInstance<SurvDISettings>();
                SurvDISettings.name = "SurvDISettings";
#if UNITY_EDITOR
                AssetDatabase.CreateAsset(SurvDISettings, SurvDISettingsPath);
#endif
            }
            Debugger.SetSettings(SurvDISettings);
        }
        private void InstallMonoContext()
        {
            var (go, monoContext) = GetOnScene<MonoContext>();
            if (go == null)
            {
                go = new GameObject(nameof(MonoContext));
                monoContext = go.AddComponent<MonoContext>();
            }

            if (monoContext == null)
                monoContext = go.AddComponent<MonoContext>();

            monoContext.Installing(Container);
            _currentSceneMonoContext = monoContext;
        }
        private void InstallProjectContext()
        {
            var (go, projectContext) = GetOnScene<ProjectContext>();
            
            if (_isHasProjectContext)
            {
                if (go != null)
                    Destroy(go);
            }
            else
            {
                if (go == null)
                {
                    go = new GameObject(nameof(ProjectContext));
                    projectContext = go.AddComponent<ProjectContext>();
                }
                
                if (go.GetComponent<Runner>() == null)
                    go.AddComponent<Runner>().Init(Container);

                if (projectContext == null)
                    projectContext = go.GetComponent<ProjectContext>();
                
                projectContext.Installing(Container);
                
                DontDestroyOnLoad(go);
                
                _isHasProjectContext = true;
            }
        }
        
        private void OnLoadScene(Scene newScene, LoadSceneMode loadSceneMode)
        {
            ContextInit();
        }

        private void ContextInit()
        {
            InstallMonoContext();
            InstallProjectContext();
            
            Invoking();
        }
        private void Invoking()
        {
            Container.LoadSavingAll();
            Container.InvokeInjectAll();
            Container.InvokeConstructorsAll();
            
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

        public static void InjectInstaller(Installer installer)
        {
            installer.InstallingInternal(Instance.Container);
            Instance.Invoking();
        }
        public static void Inject(GameObject go)
        {
            var monoContext = Instance._currentSceneMonoContext;
          
            var list = go.GetComponents<MonoBehaviour>();
            var container = Instance.Container;

            foreach (var monoBehaviour in list)
            {
                var containerUnit = InitBeh(monoBehaviour, container);
                if (containerUnit == null) continue;
                
                InitNewInstance(containerUnit, false);
                if (monoContext != null)
                    monoContext.AddNewInstanceThisContext(containerUnit);
            }
        }
        public static void InitNewInstance(ContainerUnit containerUnit, bool isInstalling)
        {
            var container = Instance.Container;
            
            if (containerUnit.Object is MonoBehaviour monoBeh)
            {
                var go = monoBeh.gameObject;

                var destroyHandler = go.GetComponent<DestroyHandlerContainerUnit>() ?? go.AddComponent<DestroyHandlerContainerUnit>();

                destroyHandler.OnDestroyEvent += containerUnit.Dispose;
                
                if (isInstalling)
                    return;
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