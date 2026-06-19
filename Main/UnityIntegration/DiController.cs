using System.Collections.Generic;
using System.IO;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Container;
using SurvDI.Core.Services.EventControllerIntegration;
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
        public static bool CanInject => Instance != null && Instance.Container != null && Instance._currentSceneMonoContext != null;

        //Contexts
        private MonoContext _currentSceneMonoContext;
        private bool _isHasProjectContext;
        private readonly List<ContainerUnit> _initBuffer = new List<ContainerUnit>();
        private static readonly List<GameObject> RootObjectsBuffer = new List<GameObject>();

        internal SurvDISettings SurvDISettings { get; private set; }
        private const string SurvDIName = "SurvDISettings";
        private const string SurvDISettingsPath = "Assets/Resources/" + SurvDIName + ".asset";
        private static string ResourcesPath => UnityEngine.Application.dataPath + "/Resources";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void EnterPlayMode()
        {
            if (Instance != null)
                return;
            new GameObject(nameof(DiController)).AddComponent<DiController>();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnEnterPlaymode()
        {
            Instance?.Init();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

#if UNITY_EDITOR
            InitSettings();
#endif
            Debugger.Log("Init DI Controller");

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnLoadScene;

            Container = new DiContainer();
            Container.BindInstanceSingle(Container);

            ContainerInitEvents.InitEvents(Container);

            if (!Container.ContainsSingle<SavingModule>())
                Container.BindSingle<SavingModule>();

            if (!Container.ContainsSingle<EventModuleManager>())
                Container.BindInstanceSingle(new EventModuleManager());
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            SceneManager.sceneLoaded -= OnLoadScene;
        }

        private void Init()
        {
            ContextInit(SceneManager.GetActiveScene());
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
        private void InstallMonoContext(Scene scene)
        {
            var (go, monoContext) = GetOnScene<MonoContext>(scene);
            if (go == null)
            {
                go = new GameObject(nameof(MonoContext));
                SceneManager.MoveGameObjectToScene(go, scene);
                monoContext = go.AddComponent<MonoContext>();
            }

            if (monoContext == null)
                monoContext = go.AddComponent<MonoContext>();

            monoContext.Installing(Container, scene);
            _currentSceneMonoContext = monoContext;
        }
        private void InstallProjectContext(Scene scene)
        {
            var (go, projectContext) = GetOnScene<ProjectContext>(scene);

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
                    SceneManager.MoveGameObjectToScene(go, scene);
                    projectContext = go.AddComponent<ProjectContext>();
                }

                if (go.GetComponent<Runner>() == null)
                    go.AddComponent<Runner>().Init(Container);

                if (projectContext == null)
                    projectContext = go.GetComponent<ProjectContext>();

                projectContext.Installing(Container, scene);

                DontDestroyOnLoad(go);

                _isHasProjectContext = true;
            }
        }

        private void OnLoadScene(Scene newScene, LoadSceneMode loadSceneMode)
        {
            if (newScene.name.Contains("SubScene"))
                return;
            ContextInit(newScene);
        }

        private void ContextInit(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            InstallProjectContext(scene);
            InstallMonoContext(scene);

            Invoking();
        }
        private void Invoking()
        {
            Container.InitModulesAll();
            Container.InvokeInjectAll();
            Container.InvokeConstructorsAll();

            Container.FillInterfaceUnitsContainers<IPreInit>(_initBuffer);
            foreach (var containerUnit in _initBuffer)
                containerUnit.InvokePreInit();
            Container.FillInterfaceUnitsContainers<IInit>(_initBuffer);
            foreach (var containerUnit in _initBuffer)
                containerUnit.InvokeInit();
            Container.FillInterfaceUnitsContainers<IPostInit>(_initBuffer);
            foreach (var containerUnit in _initBuffer)
                containerUnit.InvokePostInit();
            _initBuffer.Clear();
        }
        private static (GameObject go, T obj) GetOnScene<T>(Scene scene) where T : class
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return (null, null);

            RootObjectsBuffer.Clear();
            scene.GetRootGameObjects(RootObjectsBuffer);
            var nameT = typeof(T).Name;
            foreach (var root in RootObjectsBuffer)
            {
                if (root.name == nameT)
                    if (root.TryGetComponent<T>(out var obj))
                    {
                        RootObjectsBuffer.Clear();
                        return (root, obj);
                    }
            }
            foreach (var root in RootObjectsBuffer)
            {
                if (root.TryGetComponent<T>(out var obj))
                {
                    RootObjectsBuffer.Clear();
                    return (root, obj);
                }
            }

            RootObjectsBuffer.Clear();
            return (null, null);
        }

        public static void InjectInstaller(Installer installer)
        {
            if (Instance == null || Instance.Container == null)
                throw new System.Exception("SurvDI is not initialized");

            installer.InstallingInternal(Instance.Container);
            Instance.Invoking();
        }
        public static void InjectGameObject(GameObject go)
        {
            if (!CanInject)
                return;

            var list = go.GetComponents<MonoBehaviour>();

            foreach (var monoBehaviour in list)
                InjectMonoBeh(monoBehaviour);
        }

        public static void InjectMonoBeh(MonoBehaviour monobeh)
        {
            if (!CanInject)
                return;

            if (monobeh == null)
                return;
            var monoContext = Instance._currentSceneMonoContext;

            var containerUnit = InitBeh(monobeh);
            if (containerUnit == null) return;

            InitNewInstance(containerUnit, false);
            monoContext.AddNewInstanceThisContext(containerUnit);
        }
        public static void InitNewInstance(ContainerUnit containerUnit, bool isInstalling)
        {
            var container = Instance.Container;

            if (containerUnit.Object is MonoBehaviour monoBeh)
            {
                var go = monoBeh.gameObject;

                var destroyHandler = go.GetComponent<DestroyHandlerContainerUnit>() ?? go.AddComponent<DestroyHandlerContainerUnit>();

                destroyHandler.Register(containerUnit);

                if (isInstalling)
                    return;
                containerUnit.InitModules();
                containerUnit.InvokeInjectsOnInit(container);
                containerUnit.InvokeAllInit();
            }
        }
        public static ContainerUnit InitBeh(object monobeh)
        {
            if (Instance == null || Instance.Container == null || monobeh == null)
                return null;

            var container = Instance.Container;
            var type = monobeh.GetType();
            var attr = (BindAttribute) type.GetCustomAttribute(typeof(BindAttribute));
            if (attr == null)
                return null;
            if (container.TryGetUnitByObject(monobeh, out var existingUnit))
                return existingUnit;

            ContainerUnit unit;
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (attr.Multy)
                unit = container.BindInstanceMulti(type, monobeh, attr.InjectMode);
            else
                unit = container.BindInstanceSingle(type, monobeh, attr.InjectMode);

            if (!string.IsNullOrEmpty(attr.Id))
                unit.WithId(attr.Id);

            return unit;
        }
        public static void InjectInstances(List<object> monoBehavs)
        {
            foreach (var beh in monoBehavs)
            {
                if (beh == null)
                    continue;
                InitBeh(beh);
            }
        }
        internal static void InjectInstances(List<MonoBehaviour> monoBehavs)
        {
            foreach (var beh in monoBehavs)
            {
                if (beh == null)
                    continue;
                InitBeh(beh);
            }
        }
    }
}
