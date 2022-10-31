using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public List<Type> Interfaces { get; } = new();
        private readonly List<Type> _constructorTypes = new();
        private readonly List<(FieldInfo fieldInfo,string id)> _injectTypes     = new();
        private readonly List<(FieldInfo fieldInfo,string id)> _injectMassTypes = new();

        public readonly List<Type> InjectMassTypes = new();
#endif
        public string Id { get; private set; }
        
        public Type BaseType { get; }
        public Type Type { get; }

        public bool CanInvokeConstructor { get; set; } = true;
        public bool CanPreInit { get; set; } = true;
        public bool CanInit { get; set; } = true;
        public bool CanPostInit { get; set; } = true;
        private bool _canLoadSave = true;
        
        public event Action OnDisposeEvent;

        private bool _isInjected;
        
        public ContainerUnit(DiContainer diContainer, Type type, InjectMode injectMode = InjectMode.All, object obj = null)
        {
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
            //Debug.Log("Constructors" + constructors.Length + ":" + type.Name);
            foreach (var constructorInfo in constructors)
            {
                _constructor = constructorInfo;
                break;
            }

            if (_constructor == null)
            {
                _constructor = Type.GetConstructor(Type.EmptyTypes);
                //Debug.Log(type.Name + ":" + _constructor);
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
            //var method = _constructor.
            //Debug.Log("PreInitContstructor: " + type.Name + 
            //          "CountTypes:" + _constructorTypes.Count + 
            //          ";CanInvokeConstr:" + CanInvokeConstructor + 
            //          ";IsNullConstructor:" + (_constructor != null) +
            //          ";Constructors:" + constructors.Length);
        
            //Interfaces
            Interfaces.AddRange(Type.GetInterfaces());
            
            //Base type
            if (Type.BaseType != typeof(object) && (injectMode == InjectMode.BaseTypeAndSelf || injectMode == InjectMode.All))
                BaseType = Type.BaseType;
                
            //InjectTypes
            var fields = Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();
            
            if (BaseType != null)
                fields.AddRange(BaseType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            
            var fieldsSingle = fields.Where(s => s.GetCustomAttribute<InjectAttribute>() != null).ToList();
            var fieldsTuple = new List<(FieldInfo f, string s)>();
            fieldsTuple.AddRange(fieldsSingle.Select(s => (s, s.GetCustomAttribute<InjectAttribute>().Id)));
            _injectTypes.AddRange(fieldsTuple.Select(s => (s.f, s.s)));
            
            var fieldsMulty = fields.Where(s => s.GetCustomAttribute<InjectMultiAttribute>() != null).ToList();
            var fieldsMultiTuple = new List<(FieldInfo f, string s)>();
            fieldsMultiTuple.AddRange(fieldsMulty.Select(s => (s, s.GetCustomAttribute<InjectMultiAttribute>().Id)));
            _injectMassTypes.AddRange(fieldsMultiTuple.Select(s => (s.f, s.s)));
            
            //InjectMassTypes
            InjectMassTypes.AddRange(fieldsMulty.Select(s => s.FieldType.GetGenericArguments()[0]));
            foreach (var elementType in InjectMassTypes)
            {
                if (diContainer.ContainersMultyNeed.ContainsKey(elementType))
                    diContainer.ContainersMultyNeed[elementType].Add(this);
                else
                    diContainer.ContainersMultyNeed.Add(elementType, new List<ContainerUnit>{this});
            }
            
            //Debug.Log("Init: " + type.Name + 
            //                ":" + _constructorTypes.Count + 
            //                ":" + CanInvokeConstructor + 
            //                ":" + (_constructor != null));
        }
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public void InvokeConstructorInit(DiContainer diContainer)
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
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public void InvokeInjectsOnInit(DiContainer diContainer)
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
                    var getObjectMethod = typeof(ContainerUnit).GetMethod("GetObject")?.MakeGenericMethod(elementType);
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
#if NET_4_6
        [System.Runtime.CompilerServices.MethodImpl (System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif
        public void AddNewMulty(Type type, ContainerUnit containerUnit)
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

        public void RemoveMulty(Type type, ContainerUnit containerUnit)
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
            if (_canLoadSave)
            {
                SavingModule.LoadAll(Type, Object);
                _canLoadSave = false;
            }
        }

        #region Reflection

        public T GetObject<T>()
        {
            return (T)Object;
        }

        public void WithId(string id)
        {
            Id = id;
        }

        #endregion

        public void Dispose()
        {
            SavingModule.SaveAll(Type, Object);
            OnDisposeEvent?.Invoke();
        }
    }
}