using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
using SurvDI.Core.Services.EventControllerIntegration;
using SurvDI.Core.Services.SavingIntegration;
using UnityEngine;

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
        private readonly List<(FieldInfo fieldInfo,string id)> _injectTypes     = new List<(FieldInfo fieldInfo,string id)>();
        private readonly List<(FieldInfo fieldInfo,string id)> _injectMassTypes = new List<(FieldInfo fieldInfo,string id)>();

        public readonly List<Type> InjectMassTypes = new List<Type>();
#else
        internal List<Type> Interfaces { get; } = new();
        private readonly List<Type> _constructorTypes = new();
        private readonly List<(FieldInfo fieldInfo,InjectAttribute atr)> _injectTypes  = new();
        private readonly List<(FieldInfo fieldInfo,InjectMultiAttribute atr)> _injectMassTypes = new();

        internal readonly List<Type> InjectMassTypes = new();
#endif
        private readonly FieldInfo _eventModuleField;

        internal string Id { get; private set; }

        public Type BaseType { get; }
        public Type Type { get; }

        public bool CanInvokeConstructor { get; set; } = true;

        private bool _canPreInit = true;
        private bool _canInit = true;
        private bool _canPostInit = true;
        private bool _canLoadSave = true;

        public event Action OnDisposeEvent;

        private bool _isInjected;

        private DiContainer _diContainer;


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

            //СonstructorTypes
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
            if (Type.BaseType != typeof(object) && (injectMode == InjectMode.BaseTypeAndSelf || injectMode == InjectMode.All))
                BaseType = Type.BaseType;

            //InjectTypes
            var allFields = Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();

            if (BaseType != null)
                allFields.AddRange(BaseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

            var multyInjectFields = GetFields<InjectMultiAttribute>();
            var singleInjectFields = GetFields<InjectAttribute>();

            _injectTypes.AddRange(GetTupleListInjects<InjectAttribute>(singleInjectFields));
            _injectMassTypes.AddRange(GetTupleListInjects<InjectMultiAttribute>(multyInjectFields));

            //EventModule
            _eventModuleField = allFields.FirstOrDefault(s => s.FieldType == typeof(EventModule));

            //InjectMassTypes
            InjectMassTypes.AddRange(multyInjectFields.Select(s => s.FieldType.GetGenericArguments()[0]));
            foreach (var elementType in InjectMassTypes)
            {
                if (diContainer.ContainersMultyNeed.ContainsKey(elementType))
                    diContainer.ContainersMultyNeed[elementType].Add(this);
                else
                    diContainer.ContainersMultyNeed.Add(elementType, new List<ContainerUnit>{this});
            }

            IEnumerable<(FieldInfo fieldInfo,T id)> GetTupleListInjects<T>(IEnumerable<FieldInfo> list) where T : InjectBaseAttribute
            {
                return list.Select(s => (s, s.GetCustomAttribute<T>()));
            }
            List<FieldInfo> GetFields<T>() where T : Attribute
            {
                return allFields.Where(s => s.GetCustomAttribute<T>() != null).ToList();
            }
        }

        internal void InvokeConstructorInit(DiContainer diContainer)
        {
            if (!CanInvokeConstructor)
                return;
            var injectNeed = new List<object>();
            foreach (var type in _constructorTypes)
            {
                if (diContainer.ContainsSingle(type))
                    injectNeed.Add(diContainer.ResolveSingle(type));
                else
                    throw new Exception("Couldn`t build:" + Type.Name + ";" + "cann`t resolve type: " + type.Name);
            }
            //Debug.Log(Type.Name + " : inited");
            _constructor.Invoke(Object,  injectNeed.ToArray());
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

                if (diContainer.ContainerSingleUnits.TryGetValue(type, out var unit))
                {
                    if (attr.Id != "" && unit.Id != attr.Id)
                        continue;
                    fieldInfo.SetValue(Object, unit.Object);
                }
                else
                {
                    if (attr.CanBeNull)
                        continue;
                    throw new Exception("Cann`t resolve type: " + type.Name + (attr.Id != "" ?"[" + attr +"]" : "") + ": For: " +  Type.Name);
                }
            }
            foreach (var (fieldInfo, attr) in _injectMassTypes)
            {
                var fieldType = fieldInfo.FieldType;
                var elementType = fieldType.GetGenericArguments()[0];

                var listSource = new List<ContainerUnit>();
                var seenUnits = new HashSet<ContainerUnit>();

                var asUnits = diContainer.ContainerAsTypeUnits;
                if (asUnits.TryGetValue(elementType, out var unit))
                    AddUnique(unit);

                var multiUnits = diContainer.ContainerMultiUnits;
                if (multiUnits.TryGetValue(elementType, out var multiUnit))
                    AddUnique(multiUnit);

                if (attr.Id != "")
                    listSource = listSource.Where(s => s.Id == attr.Id).ToList();
                if (listSource.Count > 0)
                {
                    var list = CreateList(elementType);
                    //Debug.Log(nameof(GetObject));

                    foreach (var containerUnit in listSource)
                        list.Add(containerUnit.Object);

                    fieldInfo.SetValue(Object,list);
                }

                void AddUnique(List<ContainerUnit> units)
                {
                    foreach (var containerUnit in units)
                        if (seenUnits.Add(containerUnit))
                            listSource.Add(containerUnit);
                }
            }
        }

        internal void AddNewMulti(Type type, ContainerUnit containerUnit)
        {
            foreach (var (fieldInfo, id) in _injectMassTypes)
            {
                var fieldType = fieldInfo.FieldType;
                var elementType = fieldType.GetGenericArguments()[0];
                if (type == elementType)
                {
                    var list = fieldInfo.GetValue(Object) as IList;
                    if (list == null)
                    {
                        list = CreateList(elementType);
                        fieldInfo.SetValue(Object, list);
                    }
                    list.Add(containerUnit.Object);

                    containerUnit.OnDisposeEvent += () =>
                    {
                        RemoveMulti(type, containerUnit);
                    };
                    break;
                }
            }
        }
        internal void RemoveMulti(Type type, ContainerUnit containerUnit)
        {
            var toRemove = containerUnit.Object;
            if (toRemove == null)
                return;
            foreach (var (fieldInfo, id) in _injectMassTypes)
            {
                var fieldType = fieldInfo.FieldType;
                var elementType = fieldType.GetGenericArguments()[0];
                if (type == elementType)
                {
                    var list = fieldInfo.GetValue(Object) as IList;
                    if (list == null)
                        continue;

                    if (list.Contains(toRemove))
                        list.Remove(toRemove);
                    break;
                }
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
                if (_eventModuleField.GetValue(Object) == null)
                    _eventModuleField.SetValue(Object, new EventModule());

                if (_eventModuleField.GetValue(Object) is EventModule eventModule)
                    if (_diContainer.TryResolveSingle<EventModuleManager>(out var eventModuleManager))
                        eventModule.Init(eventModuleManager);
            }
        }

        public void Dispose()
        {
            if (_eventModuleField != null)
                if (_eventModuleField.GetValue(Object) is EventModule eventModule)
                    eventModule.Dispose();

            if (BindingType == BindingType.Single)
                SavingModule.SaveAll(Type, Object);
            OnDisposeEvent?.Invoke();
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
        #region Reflection

        // ReSharper disable once UnusedMember.Global
        public T GetObject<T>()
        {
            return (T)Object;
        }
        // ReSharper disable once UnusedMember.Global
        public void WithId(string id)
        {
            Id = id;
        }

        #endregion

        private static IList CreateList(Type elementType)
        {
            return (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        }
    }
}
