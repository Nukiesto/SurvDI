using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Services.EventControllerIntegration;
using SurvDI.Core.Services.SavingIntegration;

namespace SurvDI.Core.Container
{
    public class ContainerUnit
    {
        public BindingType BindingType { get; internal set; }
        public readonly object Object;

        private readonly ConstructorInfo _constructor;

#if UNITY_2019_4
        public List<Type> Interfaces { get; } = new List<Type>();
        private readonly List<Type> _constructorTypes = new List<Type>();
        private readonly List<(FieldInfo fieldInfo, InjectAttribute attr)> _injectTypes = new List<(FieldInfo fieldInfo, InjectAttribute attr)>();
        private readonly List<(FieldInfo fieldInfo, InjectMultiAttribute attr, Type elementType)> _injectMassTypes = new List<(FieldInfo fieldInfo, InjectMultiAttribute attr, Type elementType)>();

        public readonly List<Type> InjectMassTypes = new List<Type>();
#else
        internal List<Type> Interfaces { get; } = new();
        private readonly List<Type> _constructorTypes = new();
        private readonly List<(FieldInfo fieldInfo, InjectAttribute attr)> _injectTypes = new();
        private readonly List<(FieldInfo fieldInfo, InjectMultiAttribute attr, Type elementType)> _injectMassTypes = new();

        internal readonly List<Type> InjectMassTypes = new();
#endif
        private readonly FieldInfo _eventModuleField;
        private readonly DiContainer _diContainer;

        internal string Id { get; private set; } = "";

        public Type BaseType { get; }
        public Type Type { get; }

        public bool CanInvokeConstructor { get; set; } = true;
        internal bool IsDisposed { get; private set; }

        private bool _canPreInit = true;
        private bool _canInit = true;
        private bool _canPostInit = true;
        private bool _canLoadSave = true;

        public event Action OnDisposeEvent;

        private bool _isInjected;

        internal ContainerUnit(DiContainer diContainer, Type type, InjectMode injectMode = InjectMode.All, object obj = null)
        {
            _diContainer = diContainer;
            OnDisposeEvent += () => { diContainer.RemoveUnit(this); };

            Type = type;

            //Init object
            if (obj == null)
                Object = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(Type);
            else
            {
                Object = obj;
                CanInvokeConstructor = false;
            }

            //ConstructorTypes
            var constructors = Type.GetConstructors();
            foreach (var constructorInfo in constructors)
            {
                _constructor = constructorInfo;
                break;
            }

            if (_constructor == null)
                _constructor = Type.GetConstructor(Type.EmptyTypes);

            if (_constructor != null)
            {
                var args = _constructor.GetParameters();
                foreach (var parameterInfo in args)
                    _constructorTypes.Add(parameterInfo.ParameterType);
            }

            if (_constructor == null)
                if (CanInvokeConstructor)
                    CanInvokeConstructor = false;

            //Interfaces
            Interfaces.AddRange(Type.GetInterfaces());

            //Init base type
            if (Type.BaseType != null
                && Type.BaseType != typeof(object)
                && (injectMode == InjectMode.BaseTypeAndSelf || injectMode == InjectMode.All))
                BaseType = Type.BaseType;

            //InjectTypes
            var allFields = new List<FieldInfo>(Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

            if (BaseType != null)
                allFields.AddRange(BaseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

            foreach (var fieldInfo in allFields)
            {
                if (_eventModuleField == null && fieldInfo.FieldType == typeof(EventModule))
                    _eventModuleField = fieldInfo;

                var injectMultiAttribute = fieldInfo.GetCustomAttribute<InjectMultiAttribute>();
                if (injectMultiAttribute != null)
                {
                    if (!TryGetListElementType(fieldInfo.FieldType, out var elementType))
                        throw new Exception("[InjectMulti] field must be List<T>: " + Type.Name + "." + fieldInfo.Name);

                    _injectMassTypes.Add((fieldInfo, injectMultiAttribute, elementType));
                    RegisterInjectMassType(diContainer, elementType);
                }

                var injectAttribute = fieldInfo.GetCustomAttribute<InjectAttribute>();
                if (injectAttribute != null)
                    _injectTypes.Add((fieldInfo, injectAttribute));
            }
        }

        internal void InvokeConstructorInit(DiContainer diContainer)
        {
            if (!CanInvokeConstructor)
                return;

            var injectNeed = new object[_constructorTypes.Count];
            for (var i = 0; i < _constructorTypes.Count; i++)
            {
                var type = _constructorTypes[i];
                if (diContainer.TryResolveSingle(type, out var value))
                    injectNeed[i] = value;
                else
                    throw new Exception("Couldn`t build:" + Type.Name + ";" + "cann`t resolve type: " + type.Name);
            }
            //Debug.Log(Type.Name + " : inited");
            _constructor.Invoke(Object, injectNeed);
            CanInvokeConstructor = false;
        }
        internal void InvokeInjectsOnInit(DiContainer diContainer)
        {
            if (_isInjected)
                return;
            _isInjected = true;
            foreach (var (fieldInfo, attr) in _injectTypes)
            {
                var type = fieldInfo.FieldType;

                if (diContainer.TryGetUnit(type, attr.Id, out var unit))
                {
                    fieldInfo.SetValue(Object, unit.Object);
                }
                else
                {
                    if (attr.CanBeNull)
                        continue;
                    throw new Exception("Cann`t resolve type: " + type.Name + (attr.Id != "" ?"[" + attr.Id +"]" : "") + ": For: " +  Type.Name);
                }
            }
            foreach (var (fieldInfo, attr, elementType) in _injectMassTypes)
            {
                IList list = null;

                if (diContainer.ContainerAsTypeUnits.TryGetValue(elementType, out var asUnits))
                    AddMatchingUnits(asUnits);

                if (diContainer.ContainerMultiUnits.TryGetValue(elementType, out var multiUnits))
                    AddMatchingUnits(multiUnits);

                if (list != null)
                    fieldInfo.SetValue(Object, list);

                void AddMatchingUnits(List<ContainerUnit> units)
                {
                    foreach (var containerUnit in units)
                    {
                        if (!CanAddInjectedObject(containerUnit, attr.Id))
                            continue;
                        if (list == null)
                            list = CreateList(elementType);
                        if (!list.Contains(containerUnit.Object))
                            list.Add(containerUnit.Object);
                    }
                }
            }
        }

        internal void AddNewMulti(Type type, ContainerUnit containerUnit)
        {
            if (containerUnit == null || containerUnit.IsDisposed)
                return;

            var added = false;
            foreach (var (fieldInfo, attr, elementType) in _injectMassTypes)
            {
                if (type != elementType)
                    continue;
                if (!CanAddInjectedObject(containerUnit, attr.Id))
                    continue;

                var list = fieldInfo.GetValue(Object) as IList;
                if (list == null)
                {
                    list = CreateList(elementType);
                    fieldInfo.SetValue(Object, list);
                }

                if (list.Contains(containerUnit.Object))
                    continue;

                list.Add(containerUnit.Object);
                added = true;
            }

            if (added)
            {
                containerUnit.OnDisposeEvent += () =>
                {
                    RemoveMulti(type, containerUnit);
                };
            }
        }
        internal void RemoveMulti(Type type, ContainerUnit containerUnit)
        {
            var toRemove = containerUnit.Object;
            if (toRemove == null)
                return;
            foreach (var (fieldInfo, attr, elementType) in _injectMassTypes)
            {
                if (type != elementType)
                    continue;

                var list = fieldInfo.GetValue(Object) as IList;
                if (list == null)
                    continue;

                if (list.Contains(toRemove))
                    list.Remove(toRemove);
            }
        }

        public void InitModules()
        {
            if (_canLoadSave && BindingType == BindingType.Single)
            {
                SavingModule.LoadAll(Type, Object);
                _canLoadSave = false;
            }

            if (_eventModuleField != null)
            {
                var value = _eventModuleField.GetValue(Object);
                if (value == null)
                {
                    value = new EventModule();
                    _eventModuleField.SetValue(Object, value);
                }

                if (value is EventModule eventModule)
                    if (_diContainer.TryResolveSingle<EventModuleManager>(out var eventModuleManager))
                        eventModule.Init(eventModuleManager);
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            if (_eventModuleField != null)
                if (_eventModuleField.GetValue(Object) is EventModule eventModule)
                    eventModule.Dispose();

            if (BindingType == BindingType.Single)
                SavingModule.SaveAll(Type, Object);
            OnDisposeEvent?.Invoke();
            OnDisposeEvent = null;
        }

        internal void InvokePreInit()
        {
            if (_canPreInit)
            {
                _canPreInit = false;
                if (Object is IPreInit init)
                    init.PreInit();
            }
        }
        internal void InvokeInit()
        {
            if (_canInit)
            {
                _canInit = false;
                if (Object is IInit init)
                    init.Init();
            }
        }
        internal void InvokePostInit()
        {
            if (_canPostInit)
            {
                _canPostInit = false;
                if (Object is IPostInit init)
                    init.PostInit();
            }
        }
        internal void InvokeAllInit()
        {
           InvokePreInit();
           InvokeInit();
           InvokePostInit();
        }
        internal void InvokeDisposable()
        {
            if (Object is IDisposable disposable)
                disposable.Dispose();
        }
        internal bool MatchesId(string id)
        {
            return string.IsNullOrEmpty(id) || string.Equals(Id, id, StringComparison.Ordinal);
        }
        #region Reflection

        // ReSharper disable once UnusedMember.Global
        public T GetObject<T>()
        {
            return (T)Object;
        }
        // ReSharper disable once UnusedMember.Global
        public void WithId(string id)
        {
            Id = id ?? "";
            _diContainer?.AddUnitToWaitingMultiInjects(this);
        }

        #endregion

        private void RegisterInjectMassType(DiContainer diContainer, Type elementType)
        {
            if (InjectMassTypes.Contains(elementType))
                return;

            InjectMassTypes.Add(elementType);
            if (diContainer.ContainersMultyNeed.TryGetValue(elementType, out var units))
            {
                if (!units.Contains(this))
                    units.Add(this);
            }
            else
            {
                diContainer.ContainersMultyNeed.Add(elementType, new List<ContainerUnit>{this});
            }
        }

        private bool CanAddInjectedObject(ContainerUnit containerUnit, string id)
        {
            return !containerUnit.IsDisposed && containerUnit.MatchesId(id);
        }

        private static IList CreateList(Type elementType)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        }

        private static bool TryGetListElementType(Type fieldType, out Type elementType)
        {
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = fieldType.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }
    }
}
