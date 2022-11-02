using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SurvDI.Application.Interfaces;
using SurvDI.Core.Common;
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
        private readonly List<(FieldInfo fieldInfo,string id)> _injectTypes     = new();
        private readonly List<(FieldInfo fieldInfo,string id)> _injectMassTypes = new();

        internal readonly List<Type> InjectMassTypes = new();
#endif
        
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
        
        internal ContainerUnit(DiContainer diContainer, Type type, InjectMode injectMode = InjectMode.All, object obj = null)
        {
            OnDisposeEvent += () => { diContainer.RemoveUnit(this); };
            
            Type = type;

            //Init object
            if (obj == null)
            {
                Object = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(Type);
            }
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
            {
                _constructor = Type.GetConstructor(Type.EmptyTypes);
            }
                
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

            //InjectMassTypes
            InjectMassTypes.AddRange(multyInjectFields.Select(s => s.FieldType.GetGenericArguments()[0]));
            foreach (var elementType in InjectMassTypes)
            {
                if (diContainer.ContainersMultyNeed.ContainsKey(elementType))
                    diContainer.ContainersMultyNeed[elementType].Add(this);
                else
                    diContainer.ContainersMultyNeed.Add(elementType, new List<ContainerUnit>{this});
            }

            IEnumerable<(FieldInfo fieldInfo,string id)> GetTupleListInjects<T>(IEnumerable<FieldInfo> list) where T : InjectBaseAttribute
            {
                return list.Select(s => (s, s.GetCustomAttribute<T>().Id)).Select(s => (s.s, s.Id));
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
            foreach (var (fieldInfo, id) in _injectTypes)
            {
                var type = fieldInfo.FieldType;
                
                if (diContainer.ContainerSingleUnits.ContainsKey(type))
                {
                    var unit = diContainer.ContainerSingleUnits[type];
                    if (id != "" && unit.Id != id)
                        continue;
                    fieldInfo.SetValue(Object, unit.Object);
                }
                else
                    throw new Exception("Cann`t resolve type: " + type.Name + (id != "" ?"[" + id +"]" : "") + ": For: " +  Type.Name);
            }
            foreach (var (fieldInfo, id) in _injectMassTypes)
            {
                var fieldType = fieldInfo.FieldType;
                var elementType = fieldType.GetGenericArguments()[0];
                
                var listSource = new List<ContainerUnit>();
                
                var asUnits = diContainer.ContainerAsTypeUnits;
                if (asUnits.ContainsKey(elementType))
                    listSource.AddRange(asUnits[elementType]);
                
                var multiUnits = diContainer.ContainerMultiUnits;
                if (multiUnits.ContainsKey(elementType))
                    listSource.AddRange(multiUnits[elementType]);
                
                if (id != "")
                    listSource = listSource.Where(s => s.Id == id).ToList();
                if (listSource.Count > 0)
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = Activator.CreateInstance(listType);
                    Debug.Log(nameof(GetObject));

                    var getObjectMethod = typeof(ContainerUnit).GetMethod(nameof(GetObject))?.MakeGenericMethod(elementType);
                    var methodAdd = listType.GetMethod("Add");

                    if (getObjectMethod != null && methodAdd != null)
                        foreach (var containerUnit in listSource)
                            methodAdd.Invoke(list, new[]
                            {
                                getObjectMethod.Invoke(containerUnit, new object[] { })
                            });
                   
                    fieldInfo.SetValue(Object,list);
                }
            }
        }
        
        internal void AddNewMulty(Type type, ContainerUnit containerUnit)
        {
            foreach (var (fieldInfo, id) in _injectMassTypes)
            {
                var fieldType = fieldInfo.FieldType;
                var elementType = fieldType.GetGenericArguments()[0];
                if (type == elementType)
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    
                    var list = fieldInfo.GetValue(Object);
                    if (list == null)
                    {
                        list = Activator.CreateInstance(listType);
                        fieldInfo.SetValue(Object, list);
                    }
                    var methodAdd = listType.GetMethod("Add");
                    var getObjectMethod = typeof(ContainerUnit).GetMethod("GetObject")?.MakeGenericMethod(elementType);
                    var objGet = getObjectMethod?.Invoke(containerUnit, new object[] { });
                    methodAdd?.Invoke(list, new[] {objGet});

                    containerUnit.OnDisposeEvent += () =>
                    {
                        RemoveMulty(type, containerUnit);
                    };
                    break;
                }
            }
        }
        internal void RemoveMulty(Type type, ContainerUnit containerUnit)
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
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = fieldInfo.GetValue(Object);
                    if (list == null)
                    {
                        list = Activator.CreateInstance(listType);
                        fieldInfo.SetValue(Object, list);
                        continue;
                    }
                    
                    var methodRemove = listType.GetMethod("Remove");
                    var methodContains = listType.GetMethod("Contains");
                    var contains = (bool)(methodContains?.Invoke(list, new[] {toRemove})??false);
                    if (contains)
                        methodRemove?.Invoke(list, new[] {toRemove});
                    break;
                }
            }
        }
        
        public void LoadSaveable()
        {
            if (_canLoadSave && BindingType == BindingType.Single)
            {
                SavingModule.LoadAll(Type, Object);
                _canLoadSave = false;
            }
        }
        
        public void Dispose()
        {
            if (BindingType == BindingType.Single)
                SavingModule.SaveAll(Type, Object);
            OnDisposeEvent?.Invoke();
        }

        internal void InvokePreinit()
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
           InvokePreinit();
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
    }
}